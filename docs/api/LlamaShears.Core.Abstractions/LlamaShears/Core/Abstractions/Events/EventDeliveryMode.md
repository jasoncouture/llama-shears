# LlamaShears.Core.Abstractions.Events.EventDeliveryMode

Assembly: `LlamaShears.Core.Abstractions`

Delivery semantics chosen by a subscriber. Each event is dispatched
twice — once per mode — so subscribers can mix the two without the
publisher needing to know who is on which side.

## Fields

### `Awaited`

The publisher's `PublishAsync` awaits the handler before returning.

### `FireAndForget`

The publisher does not wait for the handler to complete.

