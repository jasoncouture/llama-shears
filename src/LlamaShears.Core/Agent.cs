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

public sealed partial class Agent : IAgent, IEventHandler<ChannelMessage>, IEventHandler<AgentInterruptRequest>, IAsyncDisposable
{
    private const string DefaultChannel = "default";

    private readonly ILogger _logger;
    private readonly IContextStore _contextStore;
    private readonly TimeProvider _time;
    private readonly IDisposable _subscription;
    private readonly IDisposable _interruptSubscription;
    private readonly ISessionQueue _sessionQueue;
    private readonly CancellationTokenSource _shutdown;
    private Task? _loop;
    private readonly Lock _interruptLock = new Lock();
    private CancellationTokenSource? _activeTurnCancellationTokenSource;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDataContextScope _dataScope;
    private readonly IAgentLock _agentLock;
    private readonly IAgentIterationRunner _iterationRunner;
    private readonly ImmutableArray<IAgentService> _agentServices;
    private int _disposed;


    public Agent(
        IContextStore contextStore,
        ILogger<Agent> logger,
        IEventBus bus,
        TimeProvider timeProvider,
        IEventPublisher eventPublisher,
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
        _agentServices = [..agentServices];
        var agentId = _dataScope.GetAgentConfig().Id;
        _sessionQueue = sessionFactory.Get(new SessionId(agentId, DefaultChannel));
        _shutdown = new CancellationTokenSource();
        _subscription = bus.Subscribe<ChannelMessage>(
            $"{Event.WellKnown.Channel.Message}:+",
            EventDeliveryMode.Awaited,
            this);
        _interruptSubscription = bus.Subscribe<AgentInterruptRequest>(
            Event.WellKnown.Command.InterruptAgent with { Id = agentId },
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
            foreach (var agentService in _agentServices)
            {
                await agentService.StartAsync(cancellationToken);
            }

            try
            {
                await RunIterationsAsync(agentContext, shutdownToken);
            }
            finally
            {
                foreach (var agentService in _agentServices)
                {
                    using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5), _time);
                    await agentService.StopAsync(timeoutTokenSource.Token);
                }
            }
        }, cancellationToken);
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

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _subscription.Dispose();
        _interruptSubscription.Dispose();
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
        var isIdle = true; // We intentionally don't send the first idle event. Since we aren't "idle", we are "started".
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!_sessionQueue.HasQueuedMessages() && !isIdle)
                {
                    await _eventPublisher.PublishAsync(Event.WellKnown.Agent.Idle with { Id = agentId }, AgentLifecycleMarker.Instance,
                        cancellationToken);
                    isIdle = true;
                }
                var batch = await _sessionQueue.DequeueBatchAsync(cancellationToken);
                if (batch.IsDefaultOrEmpty)
                {
                    return;
                }

                if (isIdle)
                {
                    await _eventPublisher.PublishAsync(Event.WellKnown.Agent.Busy with { Id = agentId }, AgentLifecycleMarker.Instance,
                        cancellationToken);
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
