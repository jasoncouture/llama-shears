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
    /// <remarks>
    /// <para>
    /// <b>Cancellation:</b> the bus catches
    /// <see cref="OperationCanceledException"/> raised by handlers when the
    /// dispatch <see cref="System.Threading.CancellationToken"/> has been
    /// signalled and stops that handler quietly. Awaited callers still see
    /// cancellation: <c>PublishAsync</c> reasserts the token at the tail of
    /// dispatch so <c>await PublishAsync</c> throws normally when the
    /// caller's token was tripped.
    /// </para>
    /// <para>
    /// <b>Fire-and-forget contract:</b> a
    /// <see cref="EventDeliveryMode.FireAndForget"/> handler that throws
    /// anything other than the cancellation case above will fault the
    /// underlying dispatcher's detached task and crash the host on the
    /// threadpool. FAF handlers must either complete or treat the
    /// cancellation token as their only legitimate exit. Awaited handlers
    /// can throw freely; their exceptions propagate back to the caller of
    /// <c>PublishAsync</c>.
    /// </para>
    /// </remarks>
    IDisposable Subscribe<T>(
        string? pattern,
        EventDeliveryMode mode,
        IEventHandler<T> handler)
        where T : class;
}
