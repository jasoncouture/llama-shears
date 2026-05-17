# LlamaShears.Core.Abstractions.Events.Agent

## Types

- [AgentCompactionRequest](AgentCompactionRequest.md) — Payload for [Agent](../Event/WellKnown/Agent.md).`CompactionRequested` and the start/finish events around a compaction pass. [AgentCompactionRequest](AgentCompactionRequest.md).`Force` tells the compactor to bypass its usual under-budget guard.
- [AgentLifecycleMarker](AgentLifecycleMarker.md) — Empty payload for the agent lifecycle events ([Agent](../Event/WellKnown/Agent.md).`Loaded`, [Agent](../Event/WellKnown/Agent.md).`Unloaded`, [Agent](../Event/WellKnown/Agent.md).`LoadError`). Carries no data — its presence on the bus, with the agent id on [EventType](../EventType.md).`Id`, is the signal.
- [AgentMessageBase](AgentMessageBase.md) — Common shape for agent-emitted message and thought fragments flowing through the event bus. Concrete subtypes ([AgentMessageFragment](AgentMessageFragment.md), [AgentThoughtFragment](AgentThoughtFragment.md)) add stream-specific metadata.
- [AgentMessageFragment](AgentMessageFragment.md) — One streaming chunk of agent-visible text emitted as the model produces its response. Subscribers concatenate fragments in arrival order to reconstruct the final assistant message.
- [AgentThoughtFragment](AgentThoughtFragment.md) — One streaming chunk of hidden chain-of-thought emitted by a thinking-capable model. Surfaced for visibility but never replayed back into a later prompt.
- [AgentToolCallFragment](AgentToolCallFragment.md) — Event-bus payload describing a single tool call the agent is about to dispatch. Mirrors the provider-layer ToolCall record; kept distinct so consumers of the event bus don't have to depend on the provider layer.
- [AgentToolResultFragment](AgentToolResultFragment.md) — Event-bus payload describing the outcome of a single tool call. Pairs with [AgentToolCallFragment](AgentToolCallFragment.md) via `CallId` when the provider supplies one.
- [IAgentMessage](IAgentMessage.md) — Marker interface implemented by every event payload an agent emits onto the bus — fragments, lifecycle markers, compaction markers, and so on. Subscribers use it as a single subscription point for "anything an agent said" without enumerating concrete fragment types.

