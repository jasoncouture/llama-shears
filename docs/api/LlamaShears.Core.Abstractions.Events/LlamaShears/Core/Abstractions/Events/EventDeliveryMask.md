# LlamaShears.Core.Abstractions.Events.EventDeliveryMask

Assembly: `LlamaShears.Core.Abstractions.Events`

Bit-mask companion to [EventDeliveryMode](EventDeliveryMode.md) used by
publish-side filters ([IEventFilter](IEventFilter.md)) to deny one or
both delivery legs without distinguishing per-handler.

## Fields

### `Awaited`

Suppress awaited delivery.

### `Both`

Suppress both legs (fully drop the event).

### `FireAndForget`

Suppress fire-and-forget delivery.

### `None`

Allow both legs.

