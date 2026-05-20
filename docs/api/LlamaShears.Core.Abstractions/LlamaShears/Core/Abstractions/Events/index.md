# LlamaShears.Core.Abstractions.Events

## Namespaces

- [LlamaShears.Core.Abstractions.Events.Agent](Agent/index.md)
- [LlamaShears.Core.Abstractions.Events.Channel](Channel/index.md)
- [LlamaShears.Core.Abstractions.Events.Event](Event/index.md)

## Types

- [Event](Event.md) — Constants for the framework's well-known event sources and event types. Anchors the strings used in [EventType](EventType.md).`Component` and [EventType](EventType.md).`EventName` in one place so subscribers reference symbols instead of magic strings.
- [EventBusExtensions](EventBusExtensions.md) — Convenience extensions over [IEventBus](IEventBus.md).
- [EventDeliveryMask](EventDeliveryMask.md) — Bit-mask companion to [EventDeliveryMode](EventDeliveryMode.md) used by publish-side filters ([IEventFilter](IEventFilter.md)) to deny one or both delivery legs without distinguishing per-handler.
- [EventDeliveryMode](EventDeliveryMode.md) — Delivery semantics chosen by a subscriber. Each event is dispatched twice — once per mode — so subscribers can mix the two without the publisher needing to know who is on which side.
- [EventPublisherExtensions](EventPublisherExtensions.md) — Convenience extensions over [IEventBus](IEventBus.md) that generate a fresh UUIDv7 correlation id when the caller is starting a new event chain.
- [EventType](EventType.md) — Hierarchical event identifier of the form `component:eventName[:id]`. The optional third segment carries a per-instance discriminator so subscribers can pattern-match on the prefix while keeping correlation in the suffix.
- [IEventBus](IEventBus.md) — Subscription surface for the in-process event bus. Implementations are responsible for matching an event's [IEventEnvelope](IEventEnvelope-1.md).`Type` against the supplied pattern and delivering only events that satisfy both the pattern and the requested [EventDeliveryMode](EventDeliveryMode.md).
- [IEventEnvelope<T>](IEventEnvelope-1.md) — Envelope for an internal event carried over the in-process bus.
- [IEventFilter](IEventFilter.md) — Publish-side gate consulted once per [IEventBus](IEventBus.md).`PublishAsync``1` call. Each registered filter inspects the envelope and returns the set of delivery legs it wants suppressed; the bus ORs every filter's mask together and skips any leg present in the combined mask. The default posture is allow — a filter that does not care about an event returns [EventDeliveryMask](EventDeliveryMask.md).`None`.
- [IEventHandler<T>](IEventHandler-1.md) — Handles events for a single payload type. Implement this on a class when an event handler has its own state, dependencies, or lifetime; for one-shot handlers prefer the delegate overload of [IEventBus](IEventBus.md).`Subscribe``1`.

