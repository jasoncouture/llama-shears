# Eventing

LlamaShears wires its components together with a single in-process pub/sub bus. The contracts live in [`Core.Abstractions/Events/`](../../src/LlamaShears.Core.Abstractions/Events/); the implementation is a thin wrapper over MessagePipe in [`Core.Eventing/`](../../src/LlamaShears.Core.Eventing/).

## Why a bus

A bus instead of direct calls because:

- Streaming inference is a fan-out problem (UI, persistence, eager-compactor watcher, future telemetry — all want the same fragment events from `InferenceRunner`).
- Persistence has to *block* the next iteration; UI must *not*. Same event, two delivery costs.
- Reload semantics are easier when subscribers don't hold references to the agent — the agent disposes its subscription and the rest of the graph stops hearing from it. No reference graph to untangle.

## Event identity

Every event has an `EventType` (`Component:EventName[:Id]`). The string form is colon-delimited, lowercased, validated by a regex.

The `Component` is one of the well-known sources defined in [`Event.Sources`](../../src/LlamaShears.Core.Abstractions/Events/Event.cs):

| Source | Meaning |
|--------|---------|
| `agent` | Anything an agent emits about its own state or output. |
| `system` | Host-level signals (tick, startup, shutdown). The string is `system` even though the C# class is `Host` — it predates the rename and isn't worth churning. |
| `channel` | Inputs flowing toward an agent (the chat UI, future external producers). |

The well-known `EventName`s are also defined in `Event.cs`. The current set:

| Event | Payload | Source |
|-------|---------|--------|
| `system:tick` | [`SystemTick`](../../src/LlamaShears.Core.Abstractions/Agent/SystemTick.cs) | `SystemTickService` |
| `system:startup` / `system:shutdown` | (declared, not yet emitted) | — |
| `agent:turn:<id>` | [`ModelTurn`](../../src/LlamaShears.Core.Abstractions/Provider/ModelTurn.cs) (User, Assistant, Tool, Thought) | `Agent`, `InferenceRunner` |
| `agent:message:<id>` | [`AgentMessageFragment`](../../src/LlamaShears.Core.Abstractions/Events/Agent/AgentMessageFragment.cs) (text streaming, `Final` flag) | `InferenceRunner` |
| `agent:thought:<id>` | [`AgentThoughtFragment`](../../src/LlamaShears.Core.Abstractions/Events/Agent/AgentThoughtFragment.cs) | `InferenceRunner` |
| `agent:tool-call:<id>` | [`AgentToolCallFragment`](../../src/LlamaShears.Core.Abstractions/Events/Agent/AgentToolCallFragment.cs) | `InferenceRunner` |
| `agent:tool-result:<id>` | [`AgentToolResultFragment`](../../src/LlamaShears.Core.Abstractions/Events/Agent/AgentToolResultFragment.cs) | `Agent` (after dispatch) |
| `agent:compacting-started:<id>` / `agent:compacting-finished:<id>` | [`AgentCompactionMarker`](../../src/LlamaShears.Core.Abstractions/Events/Agent/AgentCompactionMarker.cs) | `ContextCompactor` |
| `agent:loaded` / `agent:unloaded` / `agent:loading-error` / `agent:busy` / `agent:idle` | (declared in `Event.WellKnown.Agent`; emit sites currently TBD) | — |
| `channel:message:<channel-id>` | [`ChannelMessage`](../../src/LlamaShears.Core.Abstractions/Events/Channel/ChannelMessage.cs) | Chat UI / external producers |
| `channel:created` / `channel:destroyed` / `channel:error` | (declared, not yet emitted) | — |

The `Id` segment is the agent id for `agent:*` events and the channel id for `channel:message:*`. It is what makes patterns like `agent:turn:claudia` work.

## Pattern matching

Subscribers register against a *pattern*, not an exact event type. Patterns are colon-delimited like event types, with two wildcard segments:

- `*` — matches **zero or more** segments.
- `+` — matches **one or more** segments.

So:

- `agent:turn` — exact match. Won't match `agent:turn:claudia`.
- `agent:turn:+` — every agent's turn events. Won't match `agent:turn` (no id segment).
- `agent:*` — every agent event, with or without an id.
- `agent:message:claudia` — exact, claudia only.

Compiled patterns are cached. Patterns without wildcards short-circuit to a string equality check; patterns with wildcards compile to a `Regex` once and reuse it. See [`PatternMatcher`](../../src/LlamaShears.Core.Eventing/PatternMatcher.cs).

## Delivery modes

Every publish is **dual-mode**: `EventBus.PublishAsync` calls MessagePipe's `Publish` once with `DeliveryMode = FireAndForget`, then once with `DeliveryMode = Awaited`. Each subscription registers a delivery mode and discards envelopes that don't match.

The two modes mean different things to the *publisher*:

- **`FireAndForget`** — the call returns as soon as MessagePipe has dispatched. Any handler exception is observable via MessagePipe's logging but does not propagate to the publisher. Use when you have to keep streaming and a slow subscriber must not become your problem.
- **`Awaited`** — the call awaits every subscriber's `HandleAsync` to complete. A throwing handler propagates back to the publisher.

Persistence (`AgentTurnContextPersister`) subscribes `Awaited`, so the agent loop blocks until a turn is on disk before it can prompt the model again. UI fragment fan-out (`agent:message:+`, `agent:thought:+`, `agent:tool-call:+`, `agent:tool-result:+`) is `FireAndForget` — slow rendering must not stall inference.

The tradeoff lands in the right place because publishers don't get to choose the awaiting policy; subscribers do. Add a UI subscriber that takes 100 ms per fragment and the agent doesn't notice. Add a persistence subscriber that takes 100 ms per turn and the next inference pauses for 100 ms.

## Subscribing

```csharp
var subscription = bus.Subscribe<AgentMessageFragment>(
    pattern: $"{Event.WellKnown.Agent.Message}:+",
    mode: EventDeliveryMode.FireAndForget,
    handler: this);
// later:
subscription.Dispose();
```

Handlers implement `IEventHandler<T> : ValueTask HandleAsync(IEventEnvelope<T>, CancellationToken)`. The envelope carries the resolved `EventType` (with id) and a `CorrelationId` that's stamped at publish time and propagated through derivative events of the same logical operation — useful for tracing a single user turn through to its tool results.

For DI-driven subscribers (auto-activated singletons that subscribe in their constructor or `StartAsync`), use `services.AddEventHandler<T>()` from `LlamaShears.Core.Eventing.Extensions` — that registers the type as a singleton with auto-activation so it's instantiated at container build time and starts subscribing immediately.

## Project boundary

A few things to keep clean:

- **Only `Core.Eventing` references MessagePipe.** Other projects work against `IEventBus` / `IEventPublisher` from `Core.Abstractions`. The dependency on the message-broker library does not escape the implementation assembly.
- **`Core.Abstractions` knows about the bus contract but not about the bus impl.** That's why `IEventBus`, `IEventPublisher`, `IEventEnvelope<T>`, `EventType`, `EventDeliveryMode`, and the well-known event-type registry all live there.
- **Abstractions' `Events/` subdirectory is also the home for event payload records.** They're contracts shared between publisher and subscribers; the implementation projects only know how to ferry them.
- **Hosting-dependent DI extensions live in `Core.Eventing.Extensions`.** That keeps `Core.Eventing` itself usable from non-host code if the need arises later (e.g. a future plugin loader).

## Two patterns to follow

When you add a new component that participates on the bus:

1. **Define the event payload as a record in `Core.Abstractions/Events/<source>/`.** It's a contract; treat it like one. No behavior, just data.
2. **Add the well-known `EventType` constant to `Event.WellKnown.<source>`** if it's a new event name. Subscribers should match against the well-known constant, not a hand-coded string.

When you subscribe:

1. **Pick the delivery mode based on what *you* need, not what the publisher wants.** If a slow handler must not stall the publisher, register `FireAndForget`. If you need backpressure on the publisher, register `Awaited`.
2. **Hold the subscription `IDisposable` and dispose it at end-of-life.** A subscription on a `BackgroundService` lives in `StartAsync` / `StopAsync`. A subscription on a transient handler is unusual; if you find yourself doing it, push the subscription up to a singleton and have it call into the transient.
