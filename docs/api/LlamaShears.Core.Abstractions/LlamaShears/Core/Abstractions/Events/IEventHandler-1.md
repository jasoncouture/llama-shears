# LlamaShears.Core.Abstractions.Events.IEventHandler<T>

Assembly: `LlamaShears.Core.Abstractions`

Handles events for a single payload type. Implement this on a class
when an event handler has its own state, dependencies, or lifetime;
for one-shot handlers prefer the delegate overload of
[IEventBus](IEventBus.md).`Subscribe``1`.

## Type Parameters

- `T` — The payload type this handler observes.

## Methods

### `HandleAsync`(IEventEnvelope<_0> envelope, CancellationToken cancellationToken)

Invoked by the bus for each delivered envelope that matches the
subscription's pattern and delivery mode.

