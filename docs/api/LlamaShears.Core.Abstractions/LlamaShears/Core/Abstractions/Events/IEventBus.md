# LlamaShears.Core.Abstractions.Events.IEventBus

Assembly: `LlamaShears.Core.Abstractions`

Subscription surface for the in-process event bus. Implementations are
responsible for matching an event's
[IEventEnvelope](IEventEnvelope`1.md).`Type` against the supplied pattern and
delivering only events that satisfy both the pattern and the requested
[EventDeliveryMode](EventDeliveryMode.md).

