namespace LlamaShears.Core.Abstractions.Events;

/// <summary>
/// Subscription surface for the in-process event bus. Implementations are
/// responsible for matching an event's
/// <see cref="IEventEnvelope{T}.Type"/> against the supplied pattern and
/// delivering only events that satisfy both the pattern and the requested
/// <see cref="EventDeliveryMode"/>.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribes <paramref name="handler"/> to events whose
    /// <see cref="IEventEnvelope{T}.Type"/> matches <paramref name="pattern"/>
    /// and which were dispatched in <paramref name="mode"/>. The returned
    /// <see cref="IDisposable"/> unsubscribes when disposed.
    /// </summary>
    /// <typeparam name="T">The payload type the handler observes.</typeparam>
    IDisposable Subscribe<T>(
        string pattern,
        EventDeliveryMode mode,
        IEventHandler<T> handler)
        where T : class;
}
