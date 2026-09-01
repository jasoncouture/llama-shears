using System.Collections.Immutable;
using System.Diagnostics;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class Agent : IAgent, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly IContextStore _contextStore;
    private readonly TimeProvider _time;
    private readonly ISessionQueue _sessionQueue;
    private readonly IEventBus _eventBus;
    private readonly IDataContextScope _dataScope;
    private readonly IAgentPipeline _pipeline;
    private readonly IAgentLifetime _lifetime;
    private readonly ImmutableArray<IAgentService> _agentServices;
    private int _disposed;
    private bool _started;
    private readonly TaskCompletionSource _loopStatus = new();
    private readonly SessionPath _sessionPath;

    public Agent(
        IContextStore contextStore,
        ILogger<Agent> logger,
        IEventBus eventBus,
        TimeProvider timeProvider,
        IDataContextScope dataScope,
        ISessionFactory sessionFactory,
        IAgentPipeline pipeline,
        IAgentLifetime lifetime,
        IEnumerable<IAgentService> agentServices)
    {
        _sessionPath = dataScope.GetSessionPath();
        _logger = logger;
        _contextStore = contextStore;
        _eventBus = eventBus;
        _time = timeProvider;
        _dataScope = dataScope;
        _pipeline = pipeline;
        _lifetime = lifetime;
        _agentServices = [.. agentServices];
        _sessionQueue = sessionFactory.Get(_sessionPath.Current);
    }

    /// <inheritdoc />
    public async Task RunAsync()
    {
        var cancellationToken = _lifetime.Stopping;
        var agentContext = await _contextStore.OpenAsync(_dataScope.GetCurrentSessionId(), cancellationToken);

        using var shutdownTimeoutCancellationTokenSource = new CancellationTokenSource();
        await PublishLifecycleEventAsync(Event.WellKnown.Agent.Starting, cancellationToken);
        try
        {
            _started = true;
            try
            {
                foreach (var agentService in _agentServices)
                {
                    await agentService.StartAsync(cancellationToken);
                }

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
        catch (Exception ex)
        {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error);
            throw;
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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _lifetime.Stop();
        if (!_started)
        {
            _loopStatus.TrySetResult();
        }

        if (_started)
        {
            await _loopStatus.Task.ConfigureAwait(false);
        }
    }

    private async Task PublishLifecycleEventAsync(EventType type, CancellationToken cancellationToken)
    {
        var agentConfig = _dataScope.GetAgentConfig();
        var sessionId = _dataScope.GetCurrentSessionId();
        var eventInformation = new AgentLifecycleEvent(agentConfig, sessionId);
        type = type with { Id = _dataScope.GetCurrentSessionId() };
        await _eventBus.PublishAsync(type, eventInformation, cancellationToken);
    }

    private async Task RunIterationsAsync(IAgentContext agentContext, CancellationToken cancellationToken)
    {
        await PublishLifecycleEventAsync(Event.WellKnown.Agent.Started, cancellationToken);
        using var loggingScope = _logger.BeginScope("{Session}", _sessionPath.Current);
        using var eventCancellationTokenSource = new CancellationTokenSource();
        await using var tokenRegistration = cancellationToken.Register(tokenSource =>
        {
            ((CancellationTokenSource)tokenSource!).CancelAfter(TimeSpan.FromSeconds(5));
        }, eventCancellationTokenSource);

        var isIdle = true;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!_sessionQueue.HasQueuedMessages() && !isIdle)
                {
                    await PublishLifecycleEventAsync(Event.WellKnown.Agent.Idle, eventCancellationTokenSource.Token);
                    isIdle = true;
                }

                var batch = await _sessionQueue.DequeueBatchAsync(cancellationToken);
                if (batch.IsDefaultOrEmpty)
                {
                    return;
                }

                if (isIdle)
                {
                    await PublishLifecycleEventAsync(Event.WellKnown.Agent.Busy, eventCancellationTokenSource.Token);
                    isIdle = false;
                }

                var context = new AgentPipelineContext(agentContext, batch, cancellationToken);
                await _pipeline.InvokeAsync(context, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogAgentStopping(_sessionPath.Current);
                return;
            }
            catch (Exception ex)
            {
                LogProcessOnceFailed(_sessionPath.Current, ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{Session}' is stopping.")]
    private partial void LogAgentStopping(SessionId session);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Agent '{Session}' failed to process turn; will retry on next signal.")]
    private partial void LogProcessOnceFailed(SessionId session, Exception ex);
}
