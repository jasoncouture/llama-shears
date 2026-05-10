# LlamaShears.Core.Abstractions.Agent

## Namespaces

- [LlamaShears.Core.Abstractions.Agent.Persistence](Persistence/index.md)
- [LlamaShears.Core.Abstractions.Agent.Sessions](Sessions/index.md)
- [LlamaShears.Core.Abstractions.Agent.Todo](Todo/index.md)

## Types

- [AgentConfig](AgentConfig.md) — Immutable on-disk configuration snapshot for one agent. Loaded from `<Data>/agents/<id>.json` by [IAgentConfigProvider](IAgentConfigProvider.md) and held for the duration of an in-flight interaction so a single turn sees one consistent configuration end-to-end.
- [AgentEmbeddingConfig](AgentEmbeddingConfig.md) — Per-agent embedding-model selection used for memory search. Asymmetric prefixes are supplied here so the framework, not the caller, knows whether to decorate "this is a query" vs "this is a document being indexed".
- [AgentMemoryConfig](AgentMemoryConfig.md) — Per-agent memory-subsystem options. Read by the memory searcher when a caller does not pass explicit per-call overrides.
- [AgentModelConfig](AgentModelConfig.md) — Per-agent language-model selection and per-call options.
- [AgentToolConfig](AgentToolConfig.md) — Per-agent tool-loop guardrails.
- [IAgent](IAgent.md) — An autonomous component that ingests input turns, drives a model, and produces output turns. Identified by [IAgent](IAgent.md).`Id`; the rest of its surface — heartbeat cadence, channels, conversation state — is internal and reachable through the services that own it (config provider, context store, message bus).
- [IAgentConfigProvider](IAgentConfigProvider.md) — Source of truth for agent configuration. Reads from the configured agents directory (`<Data>/agents/<id>.json`) and is the single read API for both "what agents exist" and "what's the config for this agent". Implementations may cache by file metadata but must reflect on-disk edits without a host restart.
- [IAgentManager](IAgentManager.md) — Read-only view onto the set of agents currently loaded by the host. Consumers can list agent ids and check whether a given id resolves to a loaded agent. The lifecycle (loading/unloading, reconciliation) is owned by the implementation and not part of this surface.
- [IAgentTokenStore](IAgentTokenStore.md) — In-process store that issues opaque single-use bearer tokens bound to an AgentInfo. Tokens are valid until consumed (via [IAgentTokenStore](IAgentTokenStore.md).`TryGetAgentInformation`) or until they expire — whichever comes first.
- [SystemTick](SystemTick.md) — Periodic host heartbeat broadcast onto the event bus. Subscribers use it as a coarse "wall-clock advanced" signal — agent idle detection, refreshes, scheduled chores — without each component running its own timer.

