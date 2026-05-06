namespace LlamaShears.Core.Abstractions.Events;

/// <summary>
/// Bit-mask companion to <see cref="EventDeliveryMode"/> used by
/// publish-side filters (<see cref="IEventFilter"/>) to deny one or
/// both delivery legs without distinguishing per-handler.
/// </summary>
[Flags]
public enum EventDeliveryMask
{
    /// <summary>Allow both legs.</summary>
    None = 0,
    /// <summary>Suppress fire-and-forget delivery.</summary>
    FireAndForget = 1,
    /// <summary>Suppress awaited delivery.</summary>
    Awaited = 2,
    /// <summary>Suppress both legs (fully drop the event).</summary>
    Both = FireAndForget | Awaited,
}
