namespace LlamaShears.Core.Abstractions.Events;

/// <summary>
/// Delivery semantics chosen by a subscriber. Each event is dispatched
/// twice — once per mode — so subscribers can mix the two without the
/// publisher needing to know who is on which side.
/// </summary>
public enum EventDeliveryMode
{
    /// <summary>The publisher does not wait for the handler to complete.</summary>
    FireAndForget,
    /// <summary>The publisher's <c>PublishAsync</c> awaits the handler before returning.</summary>
    Awaited,
}
