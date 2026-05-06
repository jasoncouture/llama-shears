namespace LlamaShears.Core.Abstractions.Events;

/// <summary>
/// Envelope for an internal event carried over the in-process bus.
/// <para>
/// <typeparamref name="T"/> is covariant so that <c>IEventEnvelope&lt;TDerived&gt;</c>
/// is assignable to <c>IEventEnvelope&lt;TBase&gt;</c>, allowing subscribers to opt
/// into a wider category (e.g. <c>IEventEnvelope&lt;object&gt;</c>) without a separate
/// untyped envelope.
/// </para>
/// </summary>
/// <typeparam name="T">The payload type carried by <see cref="Data"/>.</typeparam>
public interface IEventEnvelope<out T>
    where T : class
{
    /// <summary>
    /// Logical event type identifier. Used for routing and diagnostics; not
    /// required to correspond to <typeparamref name="T"/>'s CLR type name.
    /// </summary>
    EventType Type { get; }

    /// <summary>
    /// The delivery mode this envelope was dispatched under. Each event is
    /// emitted twice — once as <see cref="EventDeliveryMode.FireAndForget"/>
    /// and once as <see cref="EventDeliveryMode.Awaited"/> — and subscribers
    /// opt into one or the other; this property tells a handler which mode
    /// it is running under.
    /// </summary>
    EventDeliveryMode DeliveryMode { get; }

    /// <summary>
    /// The originating event's identifier. For a fresh event this is its own
    /// freshly-generated id (UUIDv7). For a response to a prior event, this
    /// is the prior event's id, so the response can be correlated back.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// The event payload. May be <see langword="null"/>.
    /// </summary>
    T? Data { get; }
}
