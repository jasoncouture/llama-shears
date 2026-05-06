using System.Text;
using System.Threading.Channels;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Events;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using MessagePipe;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class Agent : IAgent
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
    private readonly IAsyncPublisher<AgentFragmentEmitted>? _fragments;
    private readonly IContextCompactor? _compactor;
    private readonly ModelConfiguration? _modelConfiguration;
    private readonly IContextStore? _contextStore;

    public Agent(
        string id,
        AgentConfig config,
        ILanguageModel model,
        IAgentContext agentContext,
        IReadOnlyList<IInputChannel> inputChannels,
        IReadOnlyList<IOutputChannel> outputChannels,
        ILoggerFactory loggerFactory,
        IAsyncSubscriber<SystemTick> ticks,
        ISystemPromptProvider systemPromptProvider,
        TimeProvider timeProvider,
        IContextCompactor? compactor = null,
        ModelConfiguration? modelConfiguration = null,
        IContextStore? contextStore = null,
        IAsyncPublisher<AgentFragmentEmitted>? fragments = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(agentContext);
        ArgumentNullException.ThrowIfNull(inputChannels);
        ArgumentNullException.ThrowIfNull(outputChannels);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(ticks);
        ArgumentNullException.ThrowIfNull(systemPromptProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);

        Id = id;
        Config = config;
        _model = model;
        _logger = loggerFactory.CreateLogger($"{typeof(Agent).FullName}:{id}");
        _fragments = fragments;
        _agentContext = agentContext;
        _systemPrompt = systemPromptProvider;
        _time = timeProvider;
        _compactor = compactor;
        _modelConfiguration = modelConfiguration;
        _contextStore = contextStore;
        InputChannels = inputChannels;
        OutputChannels = outputChannels;
        _signal = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
        });
        _shutdown = new CancellationTokenSource();
        _subscription = ticks.Subscribe(OnTickAsync);
        _loop = Task.Run(() => RunLoopAsync(_shutdown.Token));
        _inputWaiters = [.. inputChannels.Select(c => Task.Run(() => WatchInputAsync(c, _shutdown.Token)))];
    }

    public string Id { get; }

    public AgentConfig Config { get; }

    public DateTimeOffset LastHeartbeatAt { get; private set; }

    public TimeSpan HeartbeatPeriod => Config.HeartbeatPeriod;

    public bool HeartbeatEnabled { get; set; } = true;

    public IReadOnlyList<ModelTurn> Context => _agentContext.Turns;

    public IReadOnlyList<IInputChannel> InputChannels { get; }

    public IReadOnlyList<IOutputChannel> OutputChannels { get; }

    public void Dispose()
    {
        _subscription.Dispose();
        _shutdown.Cancel();
        _signal.Writer.TryComplete();
        try
        {
            Task.WhenAll([_loop, .._inputWaiters]).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        _shutdown.Dispose();

        foreach (var input in InputChannels)
        {
            (input as IDisposable)?.Dispose();
        }
        foreach (var output in OutputChannels)
        {
            (output as IDisposable)?.Dispose();
        }
    }

    private ValueTask OnTickAsync(SystemTick tick, CancellationToken cancellationToken)
    {
        if (!HeartbeatEnabled)
        {
            return ValueTask.CompletedTask;
        }

        if (LastHeartbeatAt != default && tick.At - LastHeartbeatAt < HeartbeatPeriod)
        {
            return ValueTask.CompletedTask;
        }

        LastHeartbeatAt = tick.At;
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
        try
        {
            while (await _signal.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_signal.Reader.TryRead(out _))
                {
                }

                await ProcessOnceAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        var contextSizeBefore = _agentContext.Turns.Count;

        foreach (var input in InputChannels)
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
        prompt = await MaybeCompactAsync(prompt, cancellationToken).ConfigureAwait(false);
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
        }
        if (textStreamSeen)
        {
            await PublishFragmentAsync(
                AgentFragmentKind.Text,
                string.Empty,
                textStreamId,
                isFinal: true,
                cancellationToken).ConfigureAwait(false);
        }

        if (thinking.Length > 0)
        {
            var thoughtTurn = new ModelTurn(ModelRole.Thought, thinking.ToString(), _time.GetUtcNow());
            await _agentContext.AppendAsync(thoughtTurn, cancellationToken).ConfigureAwait(false);
            foreach (var output in OutputChannels)
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

        foreach (var output in OutputChannels)
        {
            await output.SendAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<ModelPrompt> MaybeCompactAsync(ModelPrompt prompt, CancellationToken cancellationToken)
    {
        if (_compactor is null || _modelConfiguration is null || _contextStore is null)
        {
            return prompt;
        }

        var compacted = await _compactor.CompactAsync(prompt, _model, _modelConfiguration, cancellationToken).ConfigureAwait(false);
        if (ReferenceEquals(compacted, prompt))
        {
            return prompt;
        }

        // Compaction happened: archive the old persisted context and
        // re-seed it with the rebuilt non-system turns. The system turn
        // is reconstructed per-call and never persisted.
        LogContextCompacted(_logger, Id);
        await _contextStore.ClearAsync(Id, archive: true, cancellationToken).ConfigureAwait(false);
        foreach (var turn in compacted.Turns)
        {
            if (turn.Role == ModelRole.System)
            {
                continue;
            }
            await _agentContext.AppendAsync(turn, cancellationToken).ConfigureAwait(false);
        }
        return compacted;
    }

    private async Task PublishFragmentAsync(
        AgentFragmentKind kind,
        string delta,
        Guid streamId,
        bool isFinal,
        CancellationToken cancellationToken)
    {
        if (_fragments is null)
        {
            return;
        }
        await _fragments.PublishAsync(
            new AgentFragmentEmitted(Id, kind, delta, streamId, isFinal),
            cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' received an empty response from the model.")]
    private static partial void LogEmptyResponse(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' compacted its context to fit the window.")]
    private static partial void LogContextCompacted(ILogger logger, string agentId);
}
