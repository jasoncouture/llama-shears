using System.Text;
using System.Threading.Channels;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Events;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using MessagePipe;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class Agent : IAgent, IEventHandler<SystemTick>, IEventHandler<ChannelMessage>, IDisposable
{
    private readonly ILanguageModel _model;
    private readonly ILogger _logger;
    private readonly IAgentContext _agentContext;
    private readonly ISystemPromptProvider _systemPrompt;
    private readonly TimeProvider _time;
    private readonly IDisposable _subscription;
    private readonly Channel<bool> _signal;
    private readonly CancellationTokenSource _shutdown;
    private readonly Task _loop;
    private readonly Task[] _inputWaiters;
    private readonly IReadOnlyList<IInputChannel> _inputChannels;
    private readonly IReadOnlyList<IOutputChannel> _outputChannels;
    private readonly TimeSpan _heartbeatPeriod;
    private readonly IAsyncPublisher<AgentFragmentEmitted> _fragments;
    private readonly IEventPublisher _eventPublisher;
    private readonly IContextCompactor _compactor;
    private readonly ModelConfiguration _modelConfiguration;
    private readonly IAgentContextProvider _agentContextProvider;
    private DateTimeOffset _lastHeartbeatAt;

    public Agent(
        string id,
        AgentConfig config,
        ILanguageModel model,
        IAgentContext agentContext,
        IReadOnlyList<IInputChannel> inputChannels,
        IReadOnlyList<IOutputChannel> outputChannels,
        ILoggerFactory loggerFactory,
        IEventBus bus,
        ISystemPromptProvider systemPromptProvider,
        TimeProvider timeProvider,
        IContextCompactor compactor,
        ModelConfiguration modelConfiguration,
        IAgentContextProvider agentContextProvider,
        IAsyncPublisher<AgentFragmentEmitted> fragments,
        IEventPublisher eventPublisher)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id;
        _model = model;
        _logger = loggerFactory.CreateLogger($"{typeof(Agent).FullName}:{id}");
        _fragments = fragments;
        _eventPublisher = eventPublisher;
        _agentContext = agentContext;
        _systemPrompt = systemPromptProvider;
        _time = timeProvider;
        _compactor = compactor;
        _modelConfiguration = modelConfiguration;
        _agentContextProvider = agentContextProvider;
        _inputChannels = inputChannels;
        _outputChannels = outputChannels;
        _heartbeatPeriod = config.HeartbeatPeriod;
        _signal = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
        });
        _shutdown = new CancellationTokenSource();
        _subscription = bus.Subscribe<SystemTick>(
            Event.WellKnown.Host.Tick,
            EventDeliveryMode.FireAndForget,
            this);
        _loop = Task.Run(() => RunLoopAsync(_shutdown.Token));
        _inputWaiters = [.. inputChannels.Select(c => Task.Run(() => WatchInputAsync(c, _shutdown.Token)))];
    }

    public string Id { get; }

    public void Dispose()
    {
        _subscription.Dispose();
        _shutdown.Cancel();
        _signal.Writer.TryComplete();
        try
        {
            Task.WhenAll([_loop, .. _inputWaiters]).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        _shutdown.Dispose();

        foreach (var input in _inputChannels)
        {
            (input as IDisposable)?.Dispose();
        }
        foreach (var output in _outputChannels)
        {
            (output as IDisposable)?.Dispose();
        }
    }

    public ValueTask HandleAsync(IEventEnvelope<SystemTick> envelope, CancellationToken cancellationToken)
    {
        var tick = envelope.Data;
        if (tick is null)
        {
            return ValueTask.CompletedTask;
        }
        if (_lastHeartbeatAt != default && tick.At - _lastHeartbeatAt < _heartbeatPeriod)
        {
            return ValueTask.CompletedTask;
        }

        _lastHeartbeatAt = tick.At;
        Pulse();
        return ValueTask.CompletedTask;
    }

    private void Pulse()
    {
        _signal.Writer.TryWrite(true);
    }

    private async Task WatchInputAsync(IInputChannel channel, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await channel.WaitForInputAsync(cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                Pulse();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var loggingScope = _logger.BeginScope("{AgentId}", Id);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await _signal.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
                while (_signal.Reader.TryRead(out _))
                {
                }
                var correlationId = Guid.CreateVersion7();
                using var innerLoggingScope = _logger.BeginScope("{AgentTurnId}", correlationId);
                await ProcessOnceAsync(correlationId, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogAgentStopping(Id);
                return;
            }
            catch (Exception ex)
            {
                // Anything else — timeout, network, model-side fault — must
                // not kill the loop. Log it loudly and wait for the next
                // signal. Silently swallowing here is what hid the
                // HttpClient.Timeout / streaming-deadlock failure mode for
                // multiple sessions.
                LogProcessOnceFailed(Id, ex);
            }
        }
    }

    private async Task ProcessOnceAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        var contextSizeBefore = _agentContext.Turns.Count;

        foreach (var input in _inputChannels)
        {
            await foreach (var turn in input.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                await _agentContext.AppendAsync(turn, cancellationToken).ConfigureAwait(false);
            }
        }

        var turns = _agentContext.Turns;
        if (turns.Count == contextSizeBefore)
        {
            return;
        }

        var now = _time.GetUtcNow();
        var systemTurn = new ModelTurn(ModelRole.System, _systemPrompt.Build(Id), now);
        var prompt = new ModelPrompt([systemTurn, .. turns]);
        var agentContextSnapshot = await _agentContextProvider.CreateAgentContextAsync(Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Agent context provider returned null for running agent '{Id}'.");
        prompt = await _compactor.CompactAsync(agentContextSnapshot, prompt, _model, _modelConfiguration, cancellationToken).ConfigureAwait(false);
        var thinking = new StringBuilder();
        var content = new StringBuilder();
        var thoughtStreamId = Guid.NewGuid();
        var textStreamId = Guid.NewGuid();
        var thoughtStreamSeen = false;
        var textStreamSeen = false;

        await foreach (var fragment in _model.PromptAsync(prompt, cancellationToken).ConfigureAwait(false))
        {
            switch (fragment)
            {
                case IModelThoughtResponse thought:
                    thinking.Append(thought.Content);
                    thoughtStreamSeen = true;
                    await PublishFragmentAsync(
                        AgentFragmentKind.Thought,
                        thought.Content,
                        thoughtStreamId,
                        isFinal: false,
                        cancellationToken).ConfigureAwait(false);
                    await _eventPublisher.PublishAsync(Event.WellKnown.Agent.Thought with { Id = Id }, new AgentThoughtFragment(thought.Content, false), correlationId, cancellationToken);
                        
                    break;
                case IModelTextResponse text:
                    content.Append(text.Content);
                    textStreamSeen = true;
                    await PublishFragmentAsync(
                        AgentFragmentKind.Text,
                        text.Content,
                        textStreamId,
                        isFinal: false,
                        cancellationToken).ConfigureAwait(false);
                        await _eventPublisher.PublishAsync(Event.WellKnown.Agent.Message with { Id = Id }, new AgentMessageFragment(text.Content, false), correlationId, cancellationToken);
                    break;
                case IModelCompletionResponse completion:
                    await _agentContext.AppendAsync(new ModelTokenInformationContextEntry(completion.TokenCount), cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        if (thoughtStreamSeen)
        {
            await PublishFragmentAsync(
                AgentFragmentKind.Thought,
                string.Empty,
                thoughtStreamId,
                isFinal: true,
                cancellationToken).ConfigureAwait(false);
                await _eventPublisher.PublishAsync(Event.WellKnown.Agent.Thought with { Id = Id }, new AgentThoughtFragment(string.Empty, true), correlationId, cancellationToken);
                
        }
        if (textStreamSeen)
        {
            await PublishFragmentAsync(
                AgentFragmentKind.Text,
                string.Empty,
                textStreamId,
                isFinal: true,
                cancellationToken).ConfigureAwait(false);
            await _eventPublisher.PublishAsync(Event.WellKnown.Agent.Message with { Id = Id }, new AgentMessageFragment(string.Empty, true), correlationId, cancellationToken);
        }

        if (thinking.Length > 0)
        {
            var thoughtTurn = new ModelTurn(ModelRole.Thought, thinking.ToString(), _time.GetUtcNow());
            await _agentContext.AppendAsync(thoughtTurn, cancellationToken).ConfigureAwait(false);
            await _eventPublisher.PublishAsync(Event.WellKnown.Agent.Thought with { Id = Id }, new AgentThought(thinking.ToString()), correlationId, cancellationToken);
            
            foreach (var output in _outputChannels)
            {
                await output.SendAsync(thoughtTurn, cancellationToken).ConfigureAwait(false);
            }
        }

        if (content.Length == 0)
        {
            LogEmptyResponse(_logger, Id);
            return;
        }

        var response = new ModelTurn(ModelRole.Assistant, content.ToString(), _time.GetUtcNow());
        await _agentContext.AppendAsync(response, cancellationToken).ConfigureAwait(false);
        await _eventPublisher.PublishAsync(Event.WellKnown.Agent.Message with { Id = Id }, new AgentMessage(content.ToString()), correlationId, cancellationToken);
        

        foreach (var output in _outputChannels)
        {
            await output.SendAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task PublishFragmentAsync(
        AgentFragmentKind kind,
        string delta,
        Guid streamId,
        bool isFinal,
        CancellationToken cancellationToken)
    {
        await _fragments.PublishAsync(
            new AgentFragmentEmitted(Id, kind, delta, streamId, isFinal),
            cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' received an empty response from the model.")]
    private static partial void LogEmptyResponse(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' is stopping.")]
    private partial void LogAgentStopping(string agentId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Agent '{AgentId}' failed to process turn; will retry on next signal.")]
    private partial void LogProcessOnceFailed(string agentId, Exception ex);

    public ValueTask HandleAsync(IEventEnvelope<ChannelMessage> envelope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
