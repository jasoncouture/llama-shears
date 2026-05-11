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

    private readonly string _id;
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
    private readonly IAgentContextProvider _agentContextProvider;
    private readonly ICurrentAgentAccessor _currentAgent;
    private readonly IDataContextScope _dataScope;
    private readonly ImmutableArray<ToolGroup> _tools;
    private readonly IServiceProvider _scopedServices;
    private IAsyncDisposable? _scope;


    public Agent(
        ILanguageModel model,
        IAgentContext agentContext,
        ILogger<Agent> logger,
        IEventBus bus,
        ISystemPromptProvider systemPromptProvider,
        TimeProvider timeProvider,
        IContextCompactor compactor,
        IAgentContextProvider agentContextProvider,
        IEventPublisher eventPublisher,
        ICurrentAgentAccessor currentAgent,
        IDataContextScope dataScope,
        ISessionFactory sessionFactory,
        AsyncServiceScope scope,
        ImmutableArray<ToolGroup> tools = default)
    {
        _model = model;
        _logger = logger;
        _eventPublisher = eventPublisher;
        _agentContext = agentContext;
        _systemPrompt = systemPromptProvider;
        _time = timeProvider;
        _compactor = compactor;
        _agentContextProvider = agentContextProvider;
        _currentAgent = currentAgent;
        _dataScope = dataScope;
        _tools = tools.IsDefault ? [] : tools;
        _scope = scope;
        _scopedServices = scope.ServiceProvider;
        _id = _dataScope.GetAgentConfig().Id;
        _sessionQueue = sessionFactory.Get(new SessionId(_id, DefaultChannel));
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

        _loop = Task.Run(() => RunLoopAsync(_shutdown.Token));
    }

    public string Id => _id;

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
        await _processGate.WaitAsync(cancellationToken);
        try
        {
            var turns = _agentContext.Turns;
            var systemPromptFile = _dataScope.GetAgentConfig().SystemPrompt;
            var data = _dataScope.Snapshot();
            var systemBody = await _systemPrompt.GetAsync(systemPromptFile, data, cancellationToken)
                ;
            var systemTurn = new ModelTurn(ModelRole.System, systemBody, _time.GetLocalNow());
            var prompt = new ModelPrompt([systemTurn, .. turns]);
            var snapshot = await _agentContextProvider.CreateAgentContextAsync(Id, cancellationToken)
                               .ConfigureAwait(false)
                           ?? throw new InvalidOperationException(
                               $"Agent context provider returned null for running agent '{Id}'.");
            await _compactor.CompactAsync(snapshot, prompt, _model, _dataScope.GetModelConfiguration(), force: true, cancellationToken)
                ;
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
                await _loop;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _shutdown.Dispose();
        _processGate.Dispose();

        await scope.DisposeAsync();
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
        await _sessionQueue.EnqueueAsync(turn, cancellationToken);
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var loggingScope = _logger.BeginScope("{AgentId}", Id);
        await RunIterationsAsync(cancellationToken);
    }

    private async Task RunIterationsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var batch = await _sessionQueue.DequeueBatchAsync(cancellationToken);
                if (batch.IsDefaultOrEmpty)
                {
                    return;
                }

                var correlationId = Guid.CreateVersion7();
                using var innerLoggingScope = _logger.BeginScope("{AgentTurnId}", correlationId);
                await _processGate.WaitAsync(cancellationToken);
                using var turnCancellationTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                lock (_interruptLock)
                {
                    _activeTurnCancellationTokenSource = turnCancellationTokenSource;
                }

                try
                {
                    await ProcessIterationAsync(batch, correlationId, cancellationToken,
                        turnCancellationTokenSource.Token);
                }
                catch (OperationCanceledException) when (turnCancellationTokenSource.IsCancellationRequested &&
                                                         !cancellationToken.IsCancellationRequested)
                {
                    LogTurnInterrupted(Id, correlationId);
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
        await using var bundle = _scopedServices.GetRequiredService<IServiceScopeFactory>().CreateAsyncScopeWithData();
        await bundle.ServiceScope.ApplyScopeDataAsync(cancellationToken);
        _dataScope.SetItem("channel_id", batch[^1].ChannelId);

        foreach (var turn in batch)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = Id },
                turn,
                correlationId,
                outerCancellationToken);
        }

        var systemPromptFile = _dataScope.GetAgentConfig().SystemPrompt;
        var data = _dataScope.Snapshot();
        var systemBody = await _systemPrompt.GetAsync(systemPromptFile, data, cancellationToken);
        var systemTurn = new ModelTurn(ModelRole.System, systemBody, _time.GetLocalNow());

        var agentInfo = new AgentInfo(
            AgentId: Id,
            ModelId: _dataScope.GetModelConfiguration().Id,
            ContextWindowSize: _dataScope.GetModelConfiguration().ContextLength ?? 0);
        using var agentScope = _currentAgent.BeginScope(agentInfo);

        var turns = _agentContext.Turns;
        var prompt = new ModelPrompt([systemTurn, .. turns]);
        var agentContextSnapshot =
            await _agentContextProvider.CreateAgentContextAsync(Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Agent context provider returned null for running agent '{Id}'.");
        prompt = await _compactor
            .CompactAsync(agentContextSnapshot, prompt, _model, _dataScope.GetModelConfiguration(), force: false, cancellationToken)
            ;

        var inferenceRunner = bundle.ServiceProvider.GetRequiredService<IInferenceRunner>();
        var outcome = await inferenceRunner.RunAsync(
            eventId: Id,
            model: _model,
            prompt: prompt,
            options: new PromptOptions(Tools: _tools, InjectEphemeralContext: true),
            emitTurns: true,
            correlationId: correlationId,
            cancellationToken: cancellationToken);

        using var interruptedTokenSource =
            outcome.Interrupted ? new CancellationTokenSource(_interruptFinalizeTimeout) : null;
        var publishToken = outcome.Interrupted ? interruptedTokenSource!.Token : cancellationToken;

        if (outcome.TokenCount is { } tokens)
        {
            await _agentContext.AppendAsync(new ModelTokenInformationContextEntry(tokens), publishToken)
                ;
        }

        if (outcome.Interrupted)
        {
            LogTurnInterrupted(Id, correlationId);
            return;
        }

        if (outcome.ToolCalls.IsDefaultOrEmpty)
        {
            if (outcome.Content.Length == 0)
            {
                LogEmptyResponse(Id);
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
            await _sessionQueue.EnqueueAsync(toolTurn, cancellationToken);
        }
    }

    private static readonly TimeSpan _interruptFinalizeTimeout = TimeSpan.FromSeconds(5);


    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' received an empty response from the model.")]
    private partial void LogEmptyResponse(string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' is stopping.")]
    private partial void LogAgentStopping(string agentId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Agent '{AgentId}' failed to process turn; will retry on next signal.")]
    private partial void LogProcessOnceFailed(string agentId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information,
        Message =
            "Agent '{AgentId}' turn '{CorrelationId}' interrupted; partial fragments dropped, agent remains live.")]
    private partial void LogTurnInterrupted(string agentId, Guid correlationId);
}