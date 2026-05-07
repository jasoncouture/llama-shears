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

---

## LlamaShears.Core.Abstractions.Events

- [Event](LlamaShears/Core/Abstractions/Events/Event.md)
- [EventBusExtensions](LlamaShears/Core/Abstractions/Events/EventBusExtensions.md)
- [EventDeliveryMask](LlamaShears/Core/Abstractions/Events/EventDeliveryMask.md)
- [EventDeliveryMode](LlamaShears/Core/Abstractions/Events/EventDeliveryMode.md)
- [EventPublisherExtensions](LlamaShears/Core/Abstractions/Events/EventPublisherExtensions.md)
- [EventType](LlamaShears/Core/Abstractions/Events/EventType.md)
- [IEventBus](LlamaShears/Core/Abstractions/Events/IEventBus.md)
- [IEventEnvelope<T>](LlamaShears/Core/Abstractions/Events/IEventEnvelope-1.md)
- [IEventFilter](LlamaShears/Core/Abstractions/Events/IEventFilter.md)
- [IEventHandler<T>](LlamaShears/Core/Abstractions/Events/IEventHandler-1.md)
- [IEventPublisher](LlamaShears/Core/Abstractions/Events/IEventPublisher.md)

## LlamaShears.Core.Abstractions.Events.Agent

- [AgentCompactionMarker](LlamaShears/Core/Abstractions/Events/Agent/AgentCompactionMarker.md)
- [AgentLifecycleMarker](LlamaShears/Core/Abstractions/Events/Agent/AgentLifecycleMarker.md)
- [AgentMessageBase](LlamaShears/Core/Abstractions/Events/Agent/AgentMessageBase.md)
- [AgentMessageFragment](LlamaShears/Core/Abstractions/Events/Agent/AgentMessageFragment.md)
- [AgentThoughtFragment](LlamaShears/Core/Abstractions/Events/Agent/AgentThoughtFragment.md)
- [AgentToolCallFragment](LlamaShears/Core/Abstractions/Events/Agent/AgentToolCallFragment.md)
- [AgentToolResultFragment](LlamaShears/Core/Abstractions/Events/Agent/AgentToolResultFragment.md)

## LlamaShears.Core.Abstractions.Events.Channel

- [ChannelMessage](LlamaShears/Core/Abstractions/Events/Channel/ChannelMessage.md)

## LlamaShears.Core.Abstractions.Events.Event

- [Sources](LlamaShears/Core/Abstractions/Events/Event/Sources.md)
- [WellKnown](LlamaShears/Core/Abstractions/Events/Event/WellKnown.md)

## LlamaShears.Core.Abstractions.Events.Event.WellKnown

- [Agent](LlamaShears/Core/Abstractions/Events/Event/WellKnown/Agent.md)
- [Channel](LlamaShears/Core/Abstractions/Events/Event/WellKnown/Channel.md)
- [Host](LlamaShears/Core/Abstractions/Events/Event/WellKnown/Host.md)

