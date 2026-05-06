# LlamaShears.Core.Abstractions.Events.IEventEnvelope<T>

Assembly: `LlamaShears.Core.Abstractions.Events`

Envelope for an internal event carried over the in-process bus.


`T` is covariant so that `IEventEnvelope<TDerived>`
is assignable to `IEventEnvelope<TBase>`, allowing subscribers to opt
into a wider category (e.g. `IEventEnvelope<object>`) without a separate
untyped envelope.

## Type Parameters

- `T` — The payload type carried by [IEventEnvelope](IEventEnvelope-1.md).`Data`.

## Properties

### `CorrelationId`

The originating event's identifier. For a fresh event this is its own
freshly-generated id (UUIDv7). For a response to a prior event, this
is the prior event's id, so the response can be correlated back.

### `Data`

The event payload. May be `null`.

### `DeliveryMode`

The delivery mode this envelope was dispatched under. Each event is
emitted twice — once as [EventDeliveryMode](EventDeliveryMode.md).`FireAndForget`
and once as [EventDeliveryMode](EventDeliveryMode.md).`Awaited` — and subscribers
opt into one or the other; this property tells a handler which mode
it is running under.

### `Type`

Logical event type identifier. Used for routing and diagnostics; not
required to correspond to `T`'s CLR type name.

