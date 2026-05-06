using System.Globalization;
using System.Text;
using System.Threading.Channels;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
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
    private readonly SemaphoreSlim _processGate = new(1, 1);
    private readonly IEventPublisher _eventPublisher;
    private readonly IContextCompactor _compactor;
    private readonly ModelConfiguration _modelConfiguration;
    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IInferenceRunner _inferenceRunner;

    public Agent(
        string id,
        ILanguageModel model,
        IAgentContext agentContext,
        ILoggerFactory loggerFactory,
        IEventBus bus,
        ISystemPromptProvider systemPromptProvider,
        TimeProvider timeProvider,
        IContextCompactor compactor,
        ModelConfiguration modelConfiguration,
        IAgentContextProvider agentContextProvider,
        IEventPublisher eventPublisher,
        IInferenceRunner inferenceRunner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

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
        _inferenceRunner = inferenceRunner;
        _inbound = Channel.CreateUnbounded<IEventEnvelope<ChannelMessage>>(new UnboundedChannelOptions
        {
            SingleReader = true,
        });
        _shutdown = new CancellationTokenSource();
        _subscription = bus.Subscribe(
            $"{Event.WellKnown.Channel.Message}:+",
            EventDeliveryMode.Awaited,
            this);
        _loop = Task.Run(() => RunLoopAsync(_shutdown.Token));
    }

    public string Id { get; }

    public DateTimeOffset? LastActivity
        => _agentContext.Turns is [.., var last] ? last.Timestamp : null;

    public Task LockAsync(CancellationToken cancellationToken)
        => _processGate.WaitAsync(cancellationToken);

    public ValueTask UnlockAsync()
    {
        _processGate.Release();
        return ValueTask.CompletedTask;
    }

    public async Task RequestCompactionAsync(CancellationToken cancellationToken)
    {
        await _processGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var turns = _agentContext.Turns;
            var systemTurn = new ModelTurn(ModelRole.System, _systemPrompt.Build(Id), _time.GetLocalNow());
            var prompt = new ModelPrompt([systemTurn, .. turns]);
            var snapshot = await _agentContextProvider.CreateAgentContextAsync(Id, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Agent context provider returned null for running agent '{Id}'.");
            await _compactor.CompactAsync(snapshot, prompt, _model, _modelConfiguration, force: true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _processGate.Release();
        }
    }

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
        _processGate.Dispose();
    }

    public async ValueTask HandleAsync(IEventEnvelope<ChannelMessage> envelope, CancellationToken cancellationToken)
    {
        var data = envelope.Data;
        if (data is null)
        {
            return;
        }
        if (!string.IsNullOrWhiteSpace(data.AgentId) && !string.Equals(data.AgentId, Id, StringComparison.Ordinal))
        {
            return;
        }
        await _inbound.Writer.WriteAsync(envelope, cancellationToken).ConfigureAwait(false);
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
                await _processGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await ProcessBatchAsync(batch, correlationId, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _processGate.Release();
                }
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
        var now = _time.GetLocalNow();
        var systemTurn = new ModelTurn(ModelRole.System, _systemPrompt.Build(Id), now);
        var prompt = new ModelPrompt([systemTurn, .. turns]);
        var agentContextSnapshot = await _agentContextProvider.CreateAgentContextAsync(Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Agent context provider returned null for running agent '{Id}'.");
        prompt = await _compactor.CompactAsync(agentContextSnapshot, prompt, _model, _modelConfiguration, force: false, cancellationToken).ConfigureAwait(false);

        var outcome = await _inferenceRunner.RunAsync(
            eventId: Id,
            model: _model,
            prompt: prompt,
            options: null,
            emitTurns: true,
            correlationId: correlationId,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (outcome.TokenCount is { } tokens)
        {
            await _agentContext.AppendAsync(new ModelTokenInformationContextEntry(tokens), cancellationToken).ConfigureAwait(false);
        }

        if (outcome.Content.Length == 0)
        {
            LogEmptyResponse(_logger, Id);
        }
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
