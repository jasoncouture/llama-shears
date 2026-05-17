# LlamaShears.Core.Abstractions.Agent

## Namespaces

- [LlamaShears.Core.Abstractions.Agent.Persistence](Persistence/index.md)
- [LlamaShears.Core.Abstractions.Agent.SaveAgentConfigResult](SaveAgentConfigResult/index.md)
- [LlamaShears.Core.Abstractions.Agent.Sessions](Sessions/index.md)
- [LlamaShears.Core.Abstractions.Agent.Todo](Todo/index.md)

## Types

- [AgentConfig](AgentConfig.md) — Immutable on-disk configuration snapshot for one agent. Loaded from `<Data>/agents/<id>.json` by [IAgentConfigProvider](IAgentConfigProvider.md) and held for the duration of an in-flight interaction so a single turn sees one consistent configuration end-to-end.
- [AgentConfigExtensions](AgentConfigExtensions.md) — Convenience accessors for pulling the active [AgentConfig](AgentConfig.md) off an [IDataContextScope](../Common/IDataContextScope.md) without callers having to remember the well-known key.
- [AgentConfigFile](AgentConfigFile.md) — Raw on-disk snapshot of an agent's config file: the verbatim JSON text alongside the SHA-256 hash of the file bytes at read time. Used by the editor UI as the change token for optimistic-concurrency saves.
- [AgentInfo](AgentInfo.md) — Lightweight catalog entry describing a known agent: enough metadata to render an agent in a list or pick one for routing without loading the full [AgentConfig](AgentConfig.md).
- [AgentMemoryConfig](AgentMemoryConfig.md) — Per-agent memory-subsystem options. Read by the memory searcher when a caller does not pass explicit per-call overrides.
- [AgentState](AgentState.md) — Per-turn agent state surfaced in the data context. Top-level keys in the data context are objects, not primitives, so anything an agent wants to expose to templates or downstream consumers rides under this single record.
- [AgentStateExtensions](AgentStateExtensions.md) — Convenience accessors for pulling the active [AgentState](AgentState.md) off an [IDataContextScope](../Common/IDataContextScope.md) without callers having to remember the well-known key, plus per-field shortcuts that delegate to the same lookup.
- [AgentToolConfig](AgentToolConfig.md) — Per-agent tool-loop guardrails.
- [IAgent](IAgent.md) — An autonomous component that ingests input turns, drives a model, and produces output turns. Identified by [IAgent](IAgent.md).`Id`; the rest of its surface — heartbeat cadence, channels, conversation state — is internal and reachable through the services that own it (config provider, context store, message bus).
- [IAgentConfigProvider](IAgentConfigProvider.md) — Source of truth for agent configuration. Reads from the configured agents directory (`<Data>/agents/<id>.json`) and is the single read API for both "what agents exist" and "what's the config for this agent". Implementations may cache by file metadata but must reflect on-disk edits without a host restart.
- [IAgentManager](IAgentManager.md) — Read-only view onto the set of agents currently loaded by the host. Consumers can list agent ids and check whether a given id resolves to a loaded agent. The lifecycle (loading/unloading, reconciliation) is owned by the implementation and not part of this surface.
- [IAgentStateTracker](IAgentStateTracker.md) — Writes the active [AgentState](AgentState.md) into the current data context scope. Centralizes the construction so every caller stamps the same shape (channel, event id, correlation id).
- [IAgentTokenStore](IAgentTokenStore.md) — In-process store that issues opaque single-use bearer tokens bound to an [AgentInfo](AgentInfo.md). Tokens are valid until consumed (via [IAgentTokenStore](IAgentTokenStore.md).`TryGetAgentInformation`) or until they expire — whichever comes first.
- [SaveAgentConfigResult](SaveAgentConfigResult.md) — Outcome of an [IAgentConfigProvider](IAgentConfigProvider.md).`SaveAsync` attempt. Pattern-match to handle each branch.
- [SystemTick](SystemTick.md) — Periodic host heartbeat broadcast onto the event bus. Subscribers use it as a coarse "wall-clock advanced" signal — agent idle detection, refreshes, scheduled chores — without each component running its own timer.

