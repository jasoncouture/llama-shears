namespace LlamaShears.Core.Abstractions.Events;

/// <summary>
/// Convenience extensions over <see cref="IEventBus"/>.
/// </summary>
public static class EventBusExtensions
{
    /// <summary>
    /// Subscribes a delegate as if it were an
    /// <see cref="IEventHandler{T}"/>. Disposing the returned handle
    /// unsubscribes.
    /// </summary>
    public static IDisposable Subscribe<T>(
        this IEventBus bus,
        string? pattern,
        EventDeliveryMode mode,
        Func<IEventEnvelope<T>, CancellationToken, ValueTask> handler)
        where T : class
    {
        var delegateHandler = new DelegateEventHandler<T>(handler);
        delegateHandler.SubscriptionHandle = bus.Subscribe(pattern, mode, delegateHandler);
        return delegateHandler;
    }
}
