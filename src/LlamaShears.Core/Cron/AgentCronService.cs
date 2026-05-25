using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Cron;

public sealed partial class AgentCronService : IAgentService, IEventHandler<SystemTick>
{
    private readonly ICronScheduler _scheduler;
    private readonly TimeProvider _time;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AgentCronService> _logger;
    private readonly bool _isDefaultAgent;
    private readonly string _agentId;
    private readonly SessionPath _parentSessionPath;
    private IDisposable? _subscription;

    public AgentCronService(
        IDataContextScope dataScope,
        ICronScheduler scheduler,
        TimeProvider time,
        IEventBus eventBus,
        ILogger<AgentCronService> logger)
    {
        _scheduler = scheduler;
        _time = time;
        _eventBus = eventBus;
        _logger = logger;
        _isDefaultAgent = dataScope.GetCurrentSessionId().IsDefault;
        _parentSessionPath = dataScope.GetSessionPath();
        _agentId = dataScope.GetAgentConfig().Id;
    }

    public async ValueTask HandleAsync(IEventEnvelope<SystemTick> envelope, CancellationToken cancellationToken)
    {
        try
        {
            await _scheduler.FireDueAsync(_time.GetUtcNow(), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            LogTickFailed(_agentId, ex);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_isDefaultAgent)
        {
            return Task.CompletedTask;
        }
        _subscription = _eventBus.Subscribe(
            Event.WellKnown.Host.Tick,
            EventDeliveryMode.FireAndForget,
            this);
        LogStarted(_agentId, _parentSessionPath);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_isDefaultAgent)
        {
            return Task.CompletedTask;
        }
        _subscription?.Dispose();
        _subscription = null;
        LogStopped(_agentId, _parentSessionPath);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent cron service started for agent '{AgentId}' on session {SessionPath}.")]
    private partial void LogStarted(string agentId, SessionPath sessionPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent cron service stopped for agent '{AgentId}' on session {SessionPath}.")]
    private partial void LogStopped(string agentId, SessionPath sessionPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent cron service tick failed for agent '{AgentId}'; remaining ticks continue.")]
    private partial void LogTickFailed(string agentId, Exception ex);
}
