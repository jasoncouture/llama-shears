using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;

namespace LlamaShears.Core;

public sealed class AgentShutdownService : IAgentService, IEventHandler<AgentShutdownRequest>
{
    private readonly IEventBus _bus;
    private readonly IDataContextScope _dataScope;
    private readonly IAgentLifetime _lifetime;
    private IDisposable? _subscription;

    public AgentShutdownService(
        IEventBus bus,
        IDataContextScope dataScope,
        IAgentLifetime lifetime)
    {
        _bus = bus;
        _dataScope = dataScope;
        _lifetime = lifetime;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var sessionId = _dataScope.GetCurrentSessionId();
        _subscription = DisposableList.Create()
            .And(_bus.Subscribe<AgentShutdownRequest>(
                Event.WellKnown.Command.AgentShutdown with { Id = sessionId },
                EventDeliveryMode.Awaited,
                this,
                preserveSubscriberExecutionContext: true))
            .And(_bus.Subscribe<AgentShutdownRequest>(
                Event.WellKnown.Command.AgentShutdown,
                EventDeliveryMode.Awaited,
                this,
                preserveSubscriberExecutionContext: true));
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
    public ValueTask HandleAsync(IEventEnvelope<AgentShutdownRequest> envelope, CancellationToken cancellationToken)
    {
        if (_lifetime.Stopping.IsCancellationRequested)
        {
            return ValueTask.CompletedTask;
        }

        if (envelope.Data?.SessionId is { } target
            && target != _dataScope.GetCurrentSessionId())
        {
            return ValueTask.CompletedTask;
        }

        _lifetime.Stop();
        return ValueTask.CompletedTask;
    }
}
