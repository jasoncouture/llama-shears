namespace LlamaShears.Core.Abstractions.Events;

public static class EventBusExtensions
{
    public static IDisposable Subscribe<T>(
        this IEventBus bus,
        string pattern,
        EventDeliveryMode mode,
        Func<IEventEnvelope<T>, CancellationToken, ValueTask> handler)
        where T : class
    {
        var delegateHandler = new DelegateEventHandler<T>(handler);
        delegateHandler.SubscriptionHandle = bus.Subscribe(pattern, mode, delegateHandler);
        return delegateHandler;
    }
}
