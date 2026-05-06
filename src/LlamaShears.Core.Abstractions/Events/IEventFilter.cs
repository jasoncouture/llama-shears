namespace LlamaShears.Core.Abstractions.Events;

/// <summary>
/// Publish-side gate consulted once per <see cref="IEventPublisher.PublishAsync{T}"/>
/// call. Each registered filter inspects the envelope and returns the set of
/// delivery legs it wants suppressed; the bus ORs every filter's mask together
/// and skips any leg present in the combined mask. The default posture is
/// allow — a filter that does not care about an event returns
/// <see cref="EventDeliveryMask.None"/>.
/// <para>
/// Filters see every event regardless of payload type via the covariant
/// <see cref="IEventEnvelope{T}"/> upcast to <c>object</c>. Pattern-match on
/// <see cref="IEventEnvelope{T}.Data"/> to scope behaviour to specific
/// payloads.
/// </para>
/// <para>
/// Filters must not swallow exceptions to coerce a deny; throwing
/// propagates out of the publish call (loud failure).
/// </para>
/// </summary>
public interface IEventFilter
{
    /// <summary>
    /// Returns the delivery legs this filter wants suppressed for
    /// <paramref name="envelope"/>. Return <see cref="EventDeliveryMask.None"/>
    /// to allow both legs.
    /// </summary>
    ValueTask<EventDeliveryMask> GetDeniedModesAsync(
        IEventEnvelope<object> envelope,
        CancellationToken cancellationToken);
}
