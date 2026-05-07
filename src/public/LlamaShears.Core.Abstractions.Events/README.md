# LlamaShears.Core.Abstractions.Events

Event-bus contracts and the framework's typed event payloads for [LlamaShears](https://github.com/jasoncouture/llama-shears). Anything that wants to publish or subscribe to events compiles against this; the actual MessagePipe-backed bus implementation lives in `LlamaShears.Core.Eventing` (host-private) so plugins never take a transitive MessagePipe dependency.

## Public surface

### Bus contracts

- **`IEventBus`** / **`IEventPublisher`** — pattern-matched pub/sub.
- **`IEventEnvelope`** / **`Event`** — the typed wire shape that flows through the bus.
- **`IEventHandler<T>`** — the consumer-side handler interface.
- **`IEventFilter`** — publish-side gate that can suppress delivery in either delivery mode (used today to keep noisy events out of the awaited path).
- **`EventDeliveryMode`** / **`EventDeliveryMask`** — `FireAndForget` vs `Awaited` selection.
- **`EventType`** — well-known event name constants.
- **`DelegateEventHandler`**, **`EventBusExtensions`**, **`EventPublisherExtensions`** — convenience surface.

### Framework payloads

- **`AgentMessageBase`** + fragments (`AgentMessageFragment`, `AgentThoughtFragment`, `AgentToolCallFragment`, `AgentToolResultFragment`) — streamed pieces of an agent turn.
- **`AgentCompactionMarker`** — emitted when the context store rolls a window into archive.
- **`Channel/ChannelMessage`** — inbound user / channel-adapter input.

## See also

- [Eventing](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/eventing.md)
- [Architecture overview](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/architecture.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.
