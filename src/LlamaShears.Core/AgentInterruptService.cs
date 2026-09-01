using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;

namespace LlamaShears.Core;

public sealed class AgentInterruptService : IAgentService, IEventHandler<AgentInterruptRequest>
{
    private readonly IEventBus _bus;
    private readonly IDataContextScope _dataScope;
    private readonly IActiveTurnCancellation _activeTurn;
    private IDisposable? _subscription;

    public AgentInterruptService(
        IEventBus bus,
        IDataContextScope dataScope,
        IActiveTurnCancellation activeTurn)
    {
        _bus = bus;
        _dataScope = dataScope;
        _activeTurn = activeTurn;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription = _bus.Subscribe<AgentInterruptRequest>(
            Event.WellKnown.Command.InterruptAgent with { Id = _dataScope.GetCurrentSessionId() },
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
    public ValueTask HandleAsync(IEventEnvelope<AgentInterruptRequest> envelope, CancellationToken cancellationToken)
    {
        _activeTurn.Cancel();
        return ValueTask.CompletedTask;
    }
}
