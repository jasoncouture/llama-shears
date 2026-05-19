# LlamaShears.Core.Abstractions.Events.IEventFilter

Assembly: `LlamaShears.Core.Abstractions`

Publish-side gate consulted once per [IEventBus](IEventBus.md).`PublishAsync``1`
call. Each registered filter inspects the envelope and returns the set of
delivery legs it wants suppressed; the bus ORs every filter's mask together
and skips any leg present in the combined mask. The default posture is
allow — a filter that does not care about an event returns
[EventDeliveryMask](EventDeliveryMask.md).`None`.



Filters see every event regardless of payload type via the covariant
[IEventEnvelope](IEventEnvelope-1.md) upcast to `object`. Pattern-match on
[IEventEnvelope](IEventEnvelope-1.md).`Data` to scope behaviour to specific
payloads.





Filters must not swallow exceptions to coerce a deny; throwing
propagates out of the publish call (loud failure).

## Methods

### `GetDeniedModesAsync`(IEventEnvelope<object> envelope, CancellationToken cancellationToken)

Returns the delivery legs this filter wants suppressed for
`envelope`. Return [EventDeliveryMask](EventDeliveryMask.md).`None`
to allow both legs.

