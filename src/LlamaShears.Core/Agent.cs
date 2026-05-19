using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class Agent :
    IAgent,
    IEventHandler<ChannelMessage>,
    IEventHandler<AgentInterruptRequest>,
    IEventHandler<AgentShutdownRequest>,
    IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly IContextStore _contextStore;
    private readonly TimeProvider _time;
    private readonly IDisposable _subscription;
    private readonly IDisposable _interruptSubscription;
    private readonly IDisposable _directStopSubscription;
    private readonly IDisposable _broadcastStopSubscription;
    private readonly ISessionQueue _sessionQueue;
    private readonly CancellationTokenSource _shutdown;
    private readonly Lock _interruptLock = new Lock();
    private CancellationTokenSource? _activeTurnCancellationTokenSource;
    private readonly IEventBus _eventPublisher;
    private readonly IDataContextScope _dataScope;
    private readonly IAgentLock _agentLock;
    private readonly IAgentIterationRunner _iterationRunner;
    private readonly ImmutableArray<IAgentService> _agentServices;
    private int _disposed;
    private bool _started = false;
    private readonly TaskCompletionSource _loopStatus = new TaskCompletionSource();


    public Agent(
        IContextStore contextStore,
        ILogger<Agent> logger,
        IEventBus bus,
        TimeProvider timeProvider,
        IEventBus eventPublisher,
        IDataContextScope dataScope,
        IAgentLock agentLock,
        ISessionFactory sessionFactory,
        IAgentIterationRunner iterationRunner,
        IEnumerable<IAgentService> agentServices)
    {
        _logger = logger;
        _contextStore = contextStore;
        _eventPublisher = eventPublisher;
        _time = timeProvider;
        _dataScope = dataScope;
        _agentLock = agentLock;
        _iterationRunner = iterationRunner;
        _agentServices = [.. agentServices];
        var agentId = _dataScope.GetAgentConfig().Id;
        var sessionId = _dataScope.GetCurrentSessionId();
        _sessionQueue = sessionFactory.Get(sessionId);
        _shutdown = new CancellationTokenSource();
        _subscription = bus.Subscribe<ChannelMessage>(
            $"{Event.WellKnown.Channel.Message}:+",
            EventDeliveryMode.Awaited,
            this);
        _interruptSubscription = bus.Subscribe<AgentInterruptRequest>(
            Event.WellKnown.Command.InterruptAgent with { Id = agentId },
            EventDeliveryMode.Awaited,
            this);
        _directStopSubscription = bus.Subscribe<AgentShutdownRequest>(
            $"{Event.WellKnown.Command.AgentShutdown}:{sessionId}",
            EventDeliveryMode.Awaited,
            this);

        _broadcastStopSubscription = bus.Subscribe<AgentShutdownRequest>(
            $"{Event.WellKnown.Command.AgentShutdown}",
            EventDeliveryMode.Awaited,
            this);
    }

    private async Task PublishLifecycleEventAsync(EventType type, CancellationToken cancellationToken)
    {
        var agentConfig = _dataScope.GetAgentConfig();
        var sessionId = _dataScope.GetCurrentSessionId();
        var eventInformation = new AgentLifecycleEvent(agentConfig, sessionId);
        type = type with { Id = agentConfig.Id };
        await _eventPublisher.PublishAsync(type, eventInformation, cancellationToken);
    }

    public async Task RunAsync()
    {
        var cancellationToken = _shutdown.Token;
        var agentContext = await _contextStore.OpenAsync(_dataScope.GetAgentConfig().Id, cancellationToken);
        using var shutdownTimeoutCancellationTokenSource = new CancellationTokenSource();
        await PublishLifecycleEventAsync(Event.WellKnown.Agent.Starting, cancellationToken);
        try
        {
            _started = true;
            foreach (var agentService in _agentServices)
            {
                await agentService.StartAsync(cancellationToken);
            }

            try
            {
                await RunIterationsAsync(agentContext, cancellationToken);
            }
            finally
            {
                shutdownTimeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
                await PublishLifecycleEventAsync(Event.WellKnown.Agent.Stopping, shutdownTimeoutCancellationTokenSource.Token);

                foreach (var agentService in _agentServices)
                {
                    using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5), _time);
                    await agentService.StopAsync(timeoutTokenSource.Token);
                }
            }
        }
        finally
        {
            try
            {
                await PublishLifecycleEventAsync(Event.WellKnown.Agent.Stopped, shutdownTimeoutCancellationTokenSource.Token);
            }
            finally
            {
                _loopStatus.TrySetResult();
            }
        }
    }

    public ValueTask HandleAsync(IEventEnvelope<AgentInterruptRequest> envelope, CancellationToken cancellationToken)
    {
        CancellationTokenSource? cancellationTokenSource;
        lock (_interruptLock)
        {
            cancellationTokenSource = _activeTurnCancellationTokenSource;
        }

        cancellationTokenSource?.Cancel();
        return ValueTask.CompletedTask;
    }

    public async ValueTask HandleAsync(IEventEnvelope<AgentShutdownRequest> envelope, CancellationToken cancellationToken)
    {
        if (_shutdown.IsCancellationRequested) return;
        if (envelope.Data?.SessionId is not null)
        {
            var ownSessionId = _dataScope.GetCurrentSessionId();
            if (envelope.Data.SessionId != ownSessionId)
            {
                return;
            }
        }
        await ShutdownLoopAsync(false);
    }

    private async ValueTask ShutdownLoopAsync(bool wait)
    {
        if (!_shutdown.IsCancellationRequested)
        {
            await _shutdown.CancelAsync();
        }
        if(!_started) _loopStatus.TrySetResult();
        if (_started && wait)
        {
            await _loopStatus.Task.ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        await ShutdownLoopAsync(true).ConfigureAwait(false);
        _subscription.Dispose();
        _interruptSubscription.Dispose();
        _broadcastStopSubscription.Dispose();
        _directStopSubscription.Dispose();
        _shutdown.Dispose();
    }

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
        var isIdle = true; // We intentionally don't send the first idle event. Since we aren't "idle", we are "started".
        await PublishLifecycleEventAsync(Event.WellKnown.Agent.Started, cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!_sessionQueue.HasQueuedMessages() && !isIdle)
                {
                    await PublishLifecycleEventAsync(Event.WellKnown.Agent.Idle, cancellationToken);
                    isIdle = true;
                }
                var batch = await _sessionQueue.DequeueBatchAsync(cancellationToken);
                if (batch.IsDefaultOrEmpty)
                {
                    return;
                }

                if (isIdle)
                {
                    await PublishLifecycleEventAsync(Event.WellKnown.Agent.Busy, cancellationToken);
                    isIdle = false;
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
                    var outcome = await _iterationRunner.RunAsync(
                        agentContext,
                        batch,
                        correlationId,
                        cancellationToken,
                        turnCancellationTokenSource.Token);
                    if (outcome.Interrupted)
                    {
                        LogTurnInterrupted(agentId, correlationId);
                    }
                    else
                    {
                        foreach (var toolTurn in outcome.ToolResultTurns)
                        {
                            await _sessionQueue.EnqueueAsync(toolTurn, cancellationToken);
                        }
                    }
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
