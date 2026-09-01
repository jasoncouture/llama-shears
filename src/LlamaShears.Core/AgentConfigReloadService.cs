using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentConfigReloadService : IAgentService, IEventHandler<ConfigurationChangedNotification>
{
    private readonly IEventBus _bus;
    private readonly IDataContextScope _dataScope;
    private readonly ILogger<AgentConfigReloadService> _logger;
    private IDisposable? _subscription;

    public AgentConfigReloadService(
        IEventBus bus,
        IDataContextScope dataScope,
        ILogger<AgentConfigReloadService> logger)
    {
        _bus = bus;
        _dataScope = dataScope;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription = _bus.Subscribe<ConfigurationChangedNotification>(
            Event.WellKnown.Lifecycle.Update with { Id = _dataScope.GetAgentConfig().Id },
            EventDeliveryMode.Awaited,
            this,
            preserveSubscriberExecutionContext: true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask HandleAsync(
        IEventEnvelope<ConfigurationChangedNotification> envelope,
        CancellationToken cancellationToken)
    {
        if (envelope.Data?.UpdatedConfig is { } updated)
        {
            _dataScope.SetItem(AgentConfig.DataKey, updated);
            LogConfigReloaded(_dataScope.GetCurrentSessionId(), updated.Hash);
        }

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Agent '{Session}' picked up new config (hash '{ConfigHash}').")]
    private partial void LogConfigReloaded(SessionId session, string configHash);
}
