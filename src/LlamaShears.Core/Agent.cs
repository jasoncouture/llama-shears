using System.Globalization;
using System.Text;
using System.Threading.Channels;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class Agent : IAgent, IEventHandler<ChannelMessage>, IDisposable
{
    private readonly ILanguageModel _model;
    private readonly ILogger _logger;
    private readonly IAgentContext _agentContext;
    private readonly ISystemPromptProvider _systemPrompt;
    private readonly TimeProvider _time;
    private readonly IDisposable _subscription;
    private readonly Channel<IEventEnvelope<ChannelMessage>> _inbound;
    private readonly CancellationTokenSource _shutdown;
    private readonly Task _loop;
    private readonly IEventPublisher _eventPublisher;
    private readonly IContextCompactor _compactor;
    private readonly ModelConfiguration _modelConfiguration;
    private readonly IAgentContextProvider _agentContextProvider;

    public Agent(
        string id,
        AgentConfig config,
        ILanguageModel model,
        IAgentContext agentContext,
        ILoggerFactory loggerFactory,
        IEventBus bus,
        ISystemPromptProvider systemPromptProvider,
        TimeProvider timeProvider,
        IContextCompactor compactor,
        ModelConfiguration modelConfiguration,
        IAgentContextProvider agentContextProvider,
        IEventPublisher eventPublisher)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _ = config;

        Id = id;
        _model = model;
        _logger = loggerFactory.CreateLogger($"{typeof(Agent).FullName}:{id}");
        _eventPublisher = eventPublisher;
        _agentContext = agentContext;
        _systemPrompt = systemPromptProvider;
        _time = timeProvider;
        _compactor = compactor;
        _modelConfiguration = modelConfiguration;
        _agentContextProvider = agentContextProvider;
        _inbound = Channel.CreateUnbounded<IEventEnvelope<ChannelMessage>>(new UnboundedChannelOptions
        {
            SingleReader = true,
        });
        _shutdown = new CancellationTokenSource();
        _subscription = bus.Subscribe<ChannelMessage>(
            $"{Event.WellKnown.Channel.Message}:+",
            EventDeliveryMode.Awaited,
            this);
        _loop = Task.Run(() => RunLoopAsync(_shutdown.Token));
    }

    public string Id { get; }

    public void Dispose()
    {
        _subscription.Dispose();
        _shutdown.Cancel();
        _inbound.Writer.TryComplete();
        try
        {
            _loop.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        _shutdown.Dispose();
    }

    public ValueTask HandleAsync(IEventEnvelope<ChannelMessage> envelope, CancellationToken cancellationToken)
    {
        var data = envelope.Data;
        if (data is null)
        {
            return ValueTask.CompletedTask;
        }
        if (!string.IsNullOrWhiteSpace(data.AgentId) && !string.Equals(data.AgentId, Id, StringComparison.Ordinal))
        {
            return ValueTask.CompletedTask;
        }
        _inbound.Writer.TryWrite(envelope);
        return ValueTask.CompletedTask;
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var loggingScope = _logger.BeginScope("{AgentId}", Id);
        var reader = _inbound.Reader;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
                if (!reader.TryRead(out var first))
                {
                    continue;
                }
                var batch = new List<IEventEnvelope<ChannelMessage>> { first };
                while (reader.TryPeek(out var next) && next.Type == first.Type)
                {
                    if (!reader.TryRead(out var taken))
                    {
                        break;
                    }
                    batch.Add(taken);
                }

                var correlationId = Guid.CreateVersion7();
                using var innerLoggingScope = _logger.BeginScope("{AgentTurnId}", correlationId);
                await ProcessBatchAsync(batch, correlationId, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogAgentStopping(Id);
                return;
            }
            catch (Exception ex)
            {
                LogProcessOnceFailed(Id, ex);
            }
        }
    }

    private async Task ProcessBatchAsync(
        IReadOnlyList<IEventEnvelope<ChannelMessage>> batch,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var userTurn = BuildUserTurn(batch);
        await _eventPublisher.PublishAsync(
            Event.WellKnown.Agent.Turn with { Id = Id },
            userTurn,
            correlationId,
            cancellationToken).ConfigureAwait(false);

        var turns = _agentContext.Turns;
        var now = _time.GetUtcNow();
        var systemTurn = new ModelTurn(ModelRole.System, _systemPrompt.Build(Id), now);
        var prompt = new ModelPrompt([systemTurn, .. turns]);
        var agentContextSnapshot = await _agentContextProvider.CreateAgentContextAsync(Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Agent context provider returned null for running agent '{Id}'.");
        prompt = await _compactor.CompactAsync(agentContextSnapshot, prompt, _model, _modelConfiguration, cancellationToken).ConfigureAwait(false);
        var thinking = new StringBuilder();
        var content = new StringBuilder();
        var thoughtStreamSeen = false;
        var textStreamSeen = false;

        await foreach (var fragment in _model.PromptAsync(prompt, cancellationToken).ConfigureAwait(false))
        {
            switch (fragment)
            {
                case IModelThoughtResponse thought:
                    thinking.Append(thought.Content);
                    thoughtStreamSeen = true;
                    await _eventPublisher.PublishAsync(
                        Event.WellKnown.Agent.Thought with { Id = Id },
                        new AgentThoughtFragment(thinking.ToString(), Final: false),
                        correlationId,
                        cancellationToken).ConfigureAwait(false);
                    break;
                case IModelTextResponse text:
                    content.Append(text.Content);
                    textStreamSeen = true;
                    await _eventPublisher.PublishAsync(
                        Event.WellKnown.Agent.Message with { Id = Id },
                        new AgentMessageFragment(content.ToString(), Final: false),
                        correlationId,
                        cancellationToken).ConfigureAwait(false);
                    break;
                case IModelCompletionResponse completion:
                    await _agentContext.AppendAsync(new ModelTokenInformationContextEntry(completion.TokenCount), cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        if (thoughtStreamSeen)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Thought with { Id = Id },
                new AgentThoughtFragment(thinking.ToString(), Final: true),
                correlationId,
                cancellationToken).ConfigureAwait(false);
        }
        if (textStreamSeen)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Message with { Id = Id },
                new AgentMessageFragment(content.ToString(), Final: true),
                correlationId,
                cancellationToken).ConfigureAwait(false);
        }

        if (thinking.Length > 0)
        {
            var thoughtTurn = new ModelTurn(ModelRole.Thought, thinking.ToString(), _time.GetUtcNow());
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = Id },
                thoughtTurn,
                correlationId,
                cancellationToken).ConfigureAwait(false);
        }

        if (content.Length == 0)
        {
            LogEmptyResponse(_logger, Id);
            return;
        }

        var response = new ModelTurn(ModelRole.Assistant, content.ToString(), _time.GetUtcNow());
        await _eventPublisher.PublishAsync(
            Event.WellKnown.Agent.Turn with { Id = Id },
            response,
            correlationId,
            cancellationToken).ConfigureAwait(false);
    }

    private static ModelTurn BuildUserTurn(IReadOnlyList<IEventEnvelope<ChannelMessage>> batch)
    {
        if (batch.Count == 1)
        {
            var only = batch[0].Data!;
            return new ModelTurn(ModelRole.User, only.Text, only.Timestamp);
        }

        var sb = new StringBuilder();
        sb.Append(string.Format(
            CultureInfo.InvariantCulture,
            "The following {0} messages arrived since your last response, in order:",
            batch.Count));
        for (var i = 0; i < batch.Count; i++)
        {
            var msg = batch[i].Data!;
            sb.Append("\n\n[");
            sb.Append((i + 1).ToString(CultureInfo.InvariantCulture));
            sb.Append("] (");
            sb.Append(msg.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            sb.Append(") ");
            sb.Append(msg.Text);
        }
        return new ModelTurn(ModelRole.User, sb.ToString(), batch[^1].Data!.Timestamp);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' received an empty response from the model.")]
    private static partial void LogEmptyResponse(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' is stopping.")]
    private partial void LogAgentStopping(string agentId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Agent '{AgentId}' failed to process turn; will retry on next signal.")]
    private partial void LogProcessOnceFailed(string agentId, Exception ex);
}
