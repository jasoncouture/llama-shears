using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Persistence;

public sealed class AgentTurnContextPersister : IEventHandler<ModelTurn>, IDisposable
{
    private readonly IContextStore _store;
    private readonly IDataContextFactory _dataContextFactory;
    private readonly IDisposable _subscription;

    public AgentTurnContextPersister(IEventBus bus, IContextStore store, IDataContextFactory dataContextFactory)
    {
        _store = store;
        _dataContextFactory = dataContextFactory;
        _subscription = bus.Subscribe(
            $"{Event.WellKnown.Agent.Turn}:+",
            EventDeliveryMode.Awaited,
            this);
    }

    public async ValueTask HandleAsync(IEventEnvelope<ModelTurn> envelope, CancellationToken cancellationToken)
    {
        if (envelope.Data is null || envelope.Data.Ephemeral)
        {
            return;
        }

        var session = _dataContextFactory.Current?.TryGetSessionPath()?.Current;
        if (session is null)
        {
            return;
        }

        var context = await _store.OpenAsync(session, cancellationToken);
        await context.AppendAsync(envelope.Data, cancellationToken);
    }

    public void Dispose() => _subscription.Dispose();
}
