using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class Agent : IAgent, IEventHandler<ChannelMessage>, IAsyncDisposable
{
    private const string DefaultChannel = "default";

    private readonly AgentConfig _config;
    private readonly ILanguageModel _model;
    private readonly ILogger _logger;
    private readonly IAgentContext _agentContext;
    private readonly ISystemPromptProvider _systemPrompt;
    private readonly TimeProvider _time;
    private readonly IDisposable _subscription;
    private readonly ISessionQueue _sessionQueue;
    private readonly CancellationTokenSource _shutdown;
    private Task? _loop;
    private readonly SemaphoreSlim _processGate = new SemaphoreSlim(1, 1);
    private readonly Lock _interruptLock = new Lock();
    private CancellationTokenSource? _activeTurnCancellationTokenSource;
    private readonly IEventPublisher _eventPublisher;
    private readonly IContextCompactor _compactor;
    private readonly ModelConfiguration _modelConfiguration;
    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IInferenceRunner _inferenceRunner;
    private readonly ICurrentAgentAccessor _currentAgent;
    private readonly IDataContextFactory _dataContextFactory;
    private readonly ImmutableArray<ToolGroup> _tools;
    private readonly IServiceProvider _scopedServices;
    private IAsyncDisposable? _scope;


    public Agent(
        AgentConfig config,
        ILanguageModel model,
        IAgentContext agentContext,
        ILogger<Agent> logger,
        IEventBus bus,
        ISystemPromptProvider systemPromptProvider,
        TimeProvider timeProvider,
        IContextCompactor compactor,
        ModelConfiguration modelConfiguration,
        IAgentContextProvider agentContextProvider,
        IEventPublisher eventPublisher,
        IInferenceRunner inferenceRunner,
        ICurrentAgentAccessor currentAgent,
        IDataContextFactory dataContextFactory,
        ISessionFactory sessionFactory,
        AsyncServiceScope scope,
        ImmutableArray<ToolGroup> tools = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.Id);

        _config = config;
        _model = model;
        _logger = logger;
        _eventPublisher = eventPublisher;
        _agentContext = agentContext;
        _systemPrompt = systemPromptProvider;
        _time = timeProvider;
        _compactor = compactor;
        _modelConfiguration = modelConfiguration;
        _agentContextProvider = agentContextProvider;
        _inferenceRunner = inferenceRunner;
        _currentAgent = currentAgent;
        _dataContextFactory = dataContextFactory;
        _tools = tools.IsDefault ? [] : tools;
        _scope = scope;
        _scopedServices = scope.ServiceProvider;
        _sessionQueue = sessionFactory.Get(new SessionId(config.Id, DefaultChannel));
        _shutdown = new CancellationTokenSource();
        _subscription = bus.Subscribe(
            $"{Event.WellKnown.Channel.Message}:+",
            EventDeliveryMode.Awaited,
            this);
    }

    public void Start()
    {
        if (_loop is not null)
        {
            throw new InvalidOperationException($"Agent '{Id}' has already been started.");
        }
        // Spawn the loop with flow suppressed so the caller's AsyncLocal state
        // (including a partially-set data scope) does not leak in. The loop
        // rejoins its own scope by key as its first action.
        using (ExecutionContext.SuppressFlow())
        {
            _loop = Task.Run(() => RunLoopAsync(_shutdown.Token));
        }
    }

    public string Id => _config.Id;

    public DateTimeOffset? LastActivity
        => _agentContext.Turns is [.., var last] ? last.Timestamp : null;

    public Task LockAsync(CancellationToken cancellationToken)
        => _processGate.WaitAsync(cancellationToken);

    public ValueTask UnlockAsync()
    {
        _processGate.Release();
        return ValueTask.CompletedTask;
    }

    public Task InterruptAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationTokenSource? cancellationTokenSource;
        lock (_interruptLock)
        {
            cancellationTokenSource = _activeTurnCancellationTokenSource;
        }
        cancellationTokenSource?.Cancel();
        return Task.CompletedTask;
    }

    public async Task RequestCompactionAsync(CancellationToken cancellationToken)
    {
        await _processGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var turns = _agentContext.Turns;
            var systemBody = await _systemPrompt.GetAsync(_config.SystemPrompt, SnapshotDataScope(), cancellationToken).ConfigureAwait(false);
            var systemTurn = new ModelTurn(ModelRole.System, systemBody, _time.GetLocalNow());
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

    public async ValueTask DisposeAsync()
    {
        var scope = Interlocked.Exchange(ref _scope, null);
        if (scope is null)
        {
            return;
        }

        _subscription.Dispose();
        _shutdown.Cancel();
        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
        _shutdown.Dispose();
        _processGate.Dispose();

        await scope.DisposeAsync().ConfigureAwait(false);
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

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
        var turn = new ModelTurn(
            ModelRole.User,
            data.Text,
            data.Timestamp,
            ChannelId: envelope.Type.Id)
        {
            Attachments = data.Attachments,
        };
        await _sessionQueue.EnqueueAsync(turn, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        if (!_dataContextFactory.TryJoinContextScope(Id, out _))
        {
            throw new InvalidOperationException($"Data context for agent '{Id}' is not registered; cannot start the loop.");
        }
        using var loggingScope = _logger.BeginScope("{AgentId}", Id);
        await RunIterationsAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RunIterationsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var batch = await _sessionQueue.DequeueBatchAsync(cancellationToken).ConfigureAwait(false);
                if (batch.IsDefaultOrEmpty)
                {
                    return;
                }

                var correlationId = Guid.CreateVersion7();
                using var innerLoggingScope = _logger.BeginScope("{AgentTurnId}", correlationId);
                await _processGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                using var turnCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                lock (_interruptLock)
                {
                    _activeTurnCancellationTokenSource = turnCancellationTokenSource;
                }
                try
                {
                    await ProcessIterationAsync(batch, correlationId, cancellationToken, turnCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (turnCancellationTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    LogTurnInterrupted(_logger, Id, correlationId);
                }
                finally
                {
                    lock (_interruptLock)
                    {
                        _activeTurnCancellationTokenSource = null;
                    }
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

    private async Task ProcessIterationAsync(
        ImmutableArray<ModelTurn> batch,
        Guid correlationId,
        CancellationToken outerCancellationToken,
        CancellationToken cancellationToken)
    {
        await using var loopScope = _scopedServices.CreateAsyncScope();

        foreach (var turn in batch)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = Id },
                turn,
                correlationId,
                outerCancellationToken).ConfigureAwait(false);
        }

        var systemBody = await _systemPrompt.GetAsync(_config.SystemPrompt, SnapshotDataScope(), cancellationToken).ConfigureAwait(false);
        var systemTurn = new ModelTurn(ModelRole.System, systemBody, _time.GetLocalNow());

        var agentInfo = new AgentInfo(
            AgentId: Id,
            ModelId: _modelConfiguration.Id,
            ContextWindowSize: _modelConfiguration.ContextLength ?? 0);
        using var agentScope = _currentAgent.BeginScope(agentInfo);

        var turns = _agentContext.Turns;
        var prompt = new ModelPrompt([systemTurn, .. turns]);
        var agentContextSnapshot = await _agentContextProvider.CreateAgentContextAsync(Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Agent context provider returned null for running agent '{Id}'.");
        prompt = await _compactor.CompactAsync(agentContextSnapshot, prompt, _model, _modelConfiguration, force: false, cancellationToken).ConfigureAwait(false);

        using var inferenceDataScope = _dataContextFactory.Current?.BeginScope();
        var outcome = await _inferenceRunner.RunAsync(
            eventId: Id,
            model: _model,
            prompt: prompt,
            options: new PromptOptions(Tools: _tools, InjectEphemeralContext: true),
            emitTurns: true,
            correlationId: correlationId,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        using var interruptedTokenSource = outcome.Interrupted ? new CancellationTokenSource(_interruptFinalizeTimeout) : null;
        var publishToken = outcome.Interrupted ? interruptedTokenSource!.Token : cancellationToken;

        if (outcome.TokenCount is { } tokens)
        {
            await _agentContext.AppendAsync(new ModelTokenInformationContextEntry(tokens), publishToken).ConfigureAwait(false);
        }

        if (outcome.Interrupted)
        {
            LogTurnInterrupted(_logger, Id, correlationId);
            return;
        }

        if (outcome.ToolCalls.IsDefaultOrEmpty)
        {
            if (outcome.Content.Length == 0)
            {
                LogEmptyResponse(_logger, Id);
            }
            return;
        }

        for (var i = 0; i < outcome.ToolCalls.Length; i++)
        {
            var toolTurn = new ModelTurn(
                ModelRole.Tool,
                outcome.ToolResults[i].Content,
                _time.GetLocalNow())
            {
                ToolCall = outcome.ToolCalls[i],
                IsError = outcome.ToolResults[i].IsError,
            };
            await _sessionQueue.EnqueueAsync(toolTurn, cancellationToken).ConfigureAwait(false);
        }
    }

    private static readonly TimeSpan _interruptFinalizeTimeout = TimeSpan.FromSeconds(5);

    private IReadOnlyDictionary<string, object?> SnapshotDataScope()
        => _dataContextFactory.Current?.Snapshot()
            ?? ImmutableDictionary.Create<string, object?>(StringComparer.OrdinalIgnoreCase);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' received an empty response from the model.")]
    private static partial void LogEmptyResponse(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' is stopping.")]
    private partial void LogAgentStopping(string agentId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Agent '{AgentId}' failed to process turn; will retry on next signal.")]
    private partial void LogProcessOnceFailed(string agentId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' turn '{CorrelationId}' interrupted; partial fragments dropped, agent remains live.")]
    private static partial void LogTurnInterrupted(ILogger logger, string agentId, Guid correlationId);
}
