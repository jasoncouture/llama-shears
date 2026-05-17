using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class Agent : IAgent, IEventHandler<ChannelMessage>, IAsyncDisposable
{
    private const string DefaultChannel = "default";
    private const int EmptyResponseRetryLimit = 3;

    private readonly ILogger _logger;
    private readonly IContextStore _contextStore;
    private readonly TimeProvider _time;
    private readonly IDisposable _subscription;
    private readonly ISessionQueue _sessionQueue;
    private readonly CancellationTokenSource _shutdown;
    private Task? _loop;
    private readonly Lock _interruptLock = new Lock();
    private CancellationTokenSource? _activeTurnCancellationTokenSource;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IDataContextScope _dataScope;
    private readonly IAgentLock _agentLock;
    private readonly IServiceScopeFactory _scopeFactory;
    private int _disposed;


    public Agent(
        IContextStore contextStore,
        ILogger<Agent> logger,
        IEventBus bus,
        TimeProvider timeProvider,
        IAgentContextProvider agentContextProvider,
        IEventPublisher eventPublisher,
        IDataContextScope dataScope,
        IAgentLock agentLock,
        ISessionFactory sessionFactory,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _contextStore = contextStore;
        _eventPublisher = eventPublisher;
        _time = timeProvider;
        _agentContextProvider = agentContextProvider;
        _dataScope = dataScope;
        _agentLock = agentLock;
        _scopeFactory = scopeFactory;
        _sessionQueue = sessionFactory.Get(new SessionId(_dataScope.GetAgentConfig().Id, DefaultChannel));
        _shutdown = new CancellationTokenSource();
        _subscription = bus.Subscribe(
            $"{Event.WellKnown.Channel.Message}:+",
            EventDeliveryMode.Awaited,
            this);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_loop is not null)
        {
            throw new InvalidOperationException($"Agent has already been started.");
        }

        var agentContext = await _contextStore.OpenAsync(_dataScope.GetAgentConfig().Id, cancellationToken);
        _loop = Task.Run(async () =>
        {
            var shutdownToken = _shutdown.Token;
            await RunIterationsAsync(agentContext, shutdownToken);
        }, cancellationToken);
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
        using var lockScope = await _agentLock.AcquireLockAsync(cancellationToken);
        await using var bundle = _scopeFactory.CreateAsyncScopeWithData();
        await bundle.ServiceScope.ApplyScopeDataAsync(cancellationToken);

        var agentContext = await _contextStore.OpenAsync(_dataScope.GetAgentConfig().Id, cancellationToken);
        var prompt = new ModelPrompt([.. agentContext.Turns]);
        var snapshot = await _agentContextProvider.CreateAgentContextAsync(_dataScope.GetAgentConfig().Id, cancellationToken)
                           .ConfigureAwait(false)
                       ?? throw new InvalidOperationException(
                           $"Agent context provider returned null for running agent '{_dataScope.GetAgentConfig().Id}'.");
        var compactor = bundle.ServiceProvider.GetRequiredService<IContextCompactor>();
        await compactor.CompactAsync(snapshot, prompt, force: true, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _subscription.Dispose();
        await _shutdown.CancelAsync();
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
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask HandleAsync(IEventEnvelope<ChannelMessage> envelope, CancellationToken cancellationToken)
    {
        var data = envelope.Data;
        if (data is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(data.AgentId) && !string.Equals(data.AgentId, _dataScope.GetAgentConfig().Id, StringComparison.Ordinal))
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

    private async Task RunIterationsAsync(IAgentContext agentContext, CancellationToken cancellationToken)
    {
        var agentId = _dataScope.GetAgentConfig().Id;
        using var loggingScope = _logger.BeginScope("{AgentId}", agentId);
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
                using var lockScope = await _agentLock.AcquireLockAsync(cancellationToken);
                using var turnCancellationTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                lock (_interruptLock)
                {
                    _activeTurnCancellationTokenSource = turnCancellationTokenSource;
                }

                try
                {
                    await ProcessIterationAsync(agentContext, batch, correlationId, cancellationToken,
                        turnCancellationTokenSource.Token);
                }
                catch (OperationCanceledException) when (turnCancellationTokenSource.IsCancellationRequested &&
                                                         !cancellationToken.IsCancellationRequested)
                {
                    LogTurnInterrupted(agentId, correlationId);
                }
                finally
                {
                    lock (_interruptLock)
                    {
                        _activeTurnCancellationTokenSource = null;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogAgentStopping(agentId);
                return;
            }
            catch (Exception ex)
            {
                LogProcessOnceFailed(agentId, ex);
            }
        }
    }

    private async Task ProcessIterationAsync(
        IAgentContext agentContext,
        ImmutableArray<ModelTurn> batch,
        Guid correlationId,
        CancellationToken outerCancellationToken,
        CancellationToken cancellationToken)
    {
        await using var bundle = _scopeFactory.CreateAsyncScopeWithData();
        await bundle.ServiceScope.ApplyScopeDataAsync(cancellationToken);
        bundle.ServiceProvider.GetRequiredService<IAgentStateTracker>()
            .SetState(batch[^1].ChannelId ?? DefaultChannel, correlationId: correlationId);
        var agentId = _dataScope.GetAgentConfig().Id;

        foreach (var turn in batch)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = agentId },
                turn,
                correlationId,
                outerCancellationToken);
        }

        var prompt = new ModelPrompt([.. agentContext.Turns]);
        var agentContextSnapshot =
            await _agentContextProvider.CreateAgentContextAsync(agentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Agent context provider returned null for running agent '{agentId}'.");
        var inferenceRunner = bundle.ServiceProvider.GetRequiredService<IInferenceRunner>();
        var serverRegistry = bundle.ServiceProvider.GetRequiredService<IModelContextProtocolServerRegistry>();
        var toolDiscovery = bundle.ServiceProvider.GetRequiredService<IModelContextProtocolToolDiscovery>();
        var compactor = bundle.ServiceProvider.GetRequiredService<IContextCompactor>();

        prompt = await compactor
            .CompactAsync(agentContextSnapshot, prompt, force: false, cancellationToken)
            ;
        var servers = serverRegistry.Resolve(_dataScope.GetAgentConfig().ModelContextProtocolServers);
        var tools = await toolDiscovery.DiscoverAsync(servers.Keys, cancellationToken);
        var systemPromptTemplate = _dataScope.GetAgentConfig().SystemPrompt;
        var promptOptions = systemPromptTemplate is null
            ? new PromptOptions(Tools: tools, InjectEphemeralContext: true, EmitTurns: true)
            : new PromptOptions(Tools: tools, InjectEphemeralContext: true, EmitTurns: true, SystemPromptTemplate: systemPromptTemplate);

        InferenceOutcome outcome;
        var emptyAttempt = 0;

        while (true)
        {
            outcome = await inferenceRunner.RunAsync(
                prompt: prompt,
                options: promptOptions,
                cancellationToken: cancellationToken);
            if (outcome.Interrupted)
            {
                break;
            }
            if (outcome.Suppressed)
            {
                break;
            }
            if (!outcome.ToolCalls.IsDefaultOrEmpty || outcome.Content.Length > 0)
            {
                break;
            }
            emptyAttempt++;
            if (emptyAttempt > EmptyResponseRetryLimit)
            {
                LogEmptyResponseGaveUp(agentId, emptyAttempt);
                break;
            }
            LogEmptyResponseRetrying(agentId, emptyAttempt);
            prompt = prompt with
            {
                Turns =
                [
                    .. prompt.Turns,
                    new ModelTurn(ModelRole.User,
                        "<SYSTEM>ERROR: You must reply with content, or a tool. Please try again. If you do not wish to respond, please reply with exactly: NO_RESPONSE</SYSTEM>",
                        _time.GetLocalNow(), prompt.Turns[^1].ChannelId)
                ]
            };
        }

        using var interruptedTokenSource =
            outcome.Interrupted ? new CancellationTokenSource(_interruptFinalizeTimeout) : null;
        var publishToken = outcome.Interrupted ? interruptedTokenSource!.Token : cancellationToken;

        if (outcome.TokenCount is { } tokens)
        {
            await agentContext.AppendAsync(new ModelTokenInformationContextEntry(tokens, _time.GetLocalNow()), publishToken)
                ;
        }

        if (outcome.Interrupted)
        {
            LogTurnInterrupted(agentId, correlationId);
            return;
        }

        if (outcome.ToolCalls.IsDefaultOrEmpty)
        {
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


    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Agent '{AgentId}' received an empty response from the model; retrying without committing the turn (attempt {Attempt}).")]
    private partial void LogEmptyResponseRetrying(string agentId, int attempt);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Agent '{AgentId}' received {Attempts} consecutive empty responses from the model; giving up on this turn.")]
    private partial void LogEmptyResponseGaveUp(string agentId, int attempts);

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