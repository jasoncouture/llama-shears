# LlamaShears.Core.Abstractions.Events.Agent

## Types

- [AgentCompactionMarker](AgentCompactionMarker.md) — Empty payload for [Agent](../Event/WellKnown/Agent.md).`CompactingStarted` / [Agent](../Event/WellKnown/Agent.md).`CompactingFinished`. Carries no data — its presence on the bus is the signal.
- [AgentMessageBase](AgentMessageBase.md) — Common shape for agent-emitted message and thought fragments flowing through the event bus. Concrete subtypes ([AgentMessageFragment](AgentMessageFragment.md), [AgentThoughtFragment](AgentThoughtFragment.md)) add stream-specific metadata.
- [AgentMessageFragment](AgentMessageFragment.md) — One streaming chunk of agent-visible text emitted as the model produces its response. Subscribers concatenate fragments in arrival order to reconstruct the final assistant message.
- [AgentThoughtFragment](AgentThoughtFragment.md) — One streaming chunk of hidden chain-of-thought emitted by a thinking-capable model. Surfaced for visibility but never replayed back into a later prompt.
- [AgentToolCallFragment](AgentToolCallFragment.md) — Event-bus payload describing a single tool call the agent is about to dispatch. Mirrors the provider-layer ToolCall record; kept distinct so consumers of the event bus don't have to depend on the provider layer.
- [AgentToolResultFragment](AgentToolResultFragment.md) — Event-bus payload describing the outcome of a single tool call. Pairs with [AgentToolCallFragment](AgentToolCallFragment.md) via `CallId` when the provider supplies one.

