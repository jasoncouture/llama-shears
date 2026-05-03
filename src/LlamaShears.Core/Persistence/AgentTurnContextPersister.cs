using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Persistence;

public sealed class AgentTurnContextPersister : IEventHandler<ModelTurn>, IDisposable
{
    private readonly IContextStore _store;
    private readonly IDisposable _subscription;

    public AgentTurnContextPersister(IEventBus bus, IContextStore store)
    {
        _store = store;
        _subscription = bus.Subscribe<ModelTurn>(
            $"{Event.WellKnown.Agent.Turn}:+",
            EventDeliveryMode.Awaited,
            this);
    }

    public async ValueTask HandleAsync(IEventEnvelope<ModelTurn> envelope, CancellationToken cancellationToken)
    {
        var agentId = envelope.Type.Id;
        if (string.IsNullOrEmpty(agentId) || envelope.Data is null)
        {
            return;
        }

        var context = await _store.OpenAsync(agentId, cancellationToken).ConfigureAwait(false);
        await context.AppendAsync(envelope.Data, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() => _subscription.Dispose();
}
