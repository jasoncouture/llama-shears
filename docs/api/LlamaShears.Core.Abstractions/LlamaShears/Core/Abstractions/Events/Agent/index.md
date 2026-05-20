# LlamaShears.Core.Abstractions.Events.Agent

## Types

- [AgentCompactionRequest](AgentCompactionRequest.md) — Payload for [Command](../Event/WellKnown/Command.md).`CompactionRequest` and the start/finish events around a compaction pass. [AgentCompactionRequest](AgentCompactionRequest.md).`Force` tells the compactor to bypass its usual under-budget guard.
- [AgentDeath](AgentDeath.md) — Singleton payload for [Lifecycle](../Event/WellKnown/Lifecycle.md).`Death`. The agent id lives on the envelope's `EventType.Id`; the payload carries no further data.
- [AgentInterruptRequest](AgentInterruptRequest.md) — Payload for [Command](../Event/WellKnown/Command.md).`InterruptAgent`. Carries no data — its presence on the bus, with the agent id on [EventType](../EventType.md).`Id`, is the signal.
- [AgentLifecycleEvent](AgentLifecycleEvent.md) — Payload carried by agent lifecycle events (`agent:starting`, `agent:started`, `agent:stopping`, `agent:stopped`) identifying which agent boot the notification refers to.
- [AgentLifecycleMarker](AgentLifecycleMarker.md) — Empty payload for the agent lifecycle events ([Agent](../Event/WellKnown/Agent.md).`Loaded`, [Agent](../Event/WellKnown/Agent.md).`Unloaded`, [Agent](../Event/WellKnown/Agent.md).`LoadError`). Carries no data — its presence on the bus, with the agent id on [EventType](../EventType.md).`Id`, is the signal.
- [AgentLoadRequest](AgentLoadRequest.md) — Payload for [Command](../Event/WellKnown/Command.md).`AgentLoad`. Carries the resolved [AgentConfig](../../Agent/AgentConfig.md) the manager should bring up (or replace an existing slot with). [EventType](../EventType.md).`Id` on the envelope holds the target agent id.
- [AgentMessageBase](AgentMessageBase.md) — Common shape for agent-emitted message and thought fragments flowing through the event bus. Concrete subtypes ([AgentMessageFragment](AgentMessageFragment.md), [AgentThoughtFragment](AgentThoughtFragment.md)) add stream-specific metadata.
- [AgentMessageFragment](AgentMessageFragment.md) — One streaming chunk of agent-visible text emitted as the model produces its response. Subscribers concatenate fragments in arrival order to reconstruct the final assistant message.
- [AgentShutdownRequest](AgentShutdownRequest.md) — Payload for [Command](../Event/WellKnown/Command.md).`AgentShutdown`. The target session shuts itself down — cancels its loop, awaits drain, publishes its own `agent:stopped` lifecycle event.
- [AgentStartRequest](AgentStartRequest.md) — Payload for [Command](../Event/WellKnown/Command.md).`AgentStart`. Hands a cold [AgentHandle](../../../AgentHandle.md) built by `IAgentFactory` off to the host, which is responsible for registering it in the repository and starting its loop.
- [AgentStopRequest](AgentStopRequest.md) — Payload for [Command](../Event/WellKnown/Command.md).`AgentStop`. Targets a specific session that the host is about to tear down; carries a non-null [AgentStopRequest](AgentStopRequest.md).`SessionId`.
- [AgentThoughtFragment](AgentThoughtFragment.md) — One streaming chunk of hidden chain-of-thought emitted by a thinking-capable model. Surfaced for visibility but never replayed back into a later prompt.
- [AgentToolCallFragment](AgentToolCallFragment.md) — Event-bus payload describing a single tool call the agent is about to dispatch. Mirrors the provider-layer ToolCall record; kept distinct so consumers of the event bus don't have to depend on the provider layer.
- [AgentToolResultFragment](AgentToolResultFragment.md) — Event-bus payload describing the outcome of a single tool call. Pairs with [AgentToolCallFragment](AgentToolCallFragment.md) via `CallId` when the provider supplies one.
- [AgentUnloadRequest](AgentUnloadRequest.md) — Payload for [Command](../Event/WellKnown/Command.md).`AgentUnload`. Carries no data — its presence on the bus, with the agent id on [EventType](../EventType.md).`Id`, is the signal.
- [ConfigurationChangedNotification](ConfigurationChangedNotification.md) — Carries both ends of a config diff so subscribers can decide between birth, tombstone, and update cases.
- [IAgentMessage](IAgentMessage.md) — Marker interface implemented by every event payload an agent emits onto the bus — fragments, lifecycle markers, compaction markers, and so on. Subscribers use it as a single subscription point for "anything an agent said" without enumerating concrete fragment types.

