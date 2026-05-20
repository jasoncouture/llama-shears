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
- [AgentInfo](AgentInfo.md) — Lightweight catalog entry describing a known agent session: enough metadata to render an agent in a list or pick one for routing without loading the full [AgentConfig](AgentConfig.md).
- [AgentMemoryConfig](AgentMemoryConfig.md) — Per-agent memory-subsystem options. Read by the memory searcher when a caller does not pass explicit per-call overrides.
- [AgentState](AgentState.md) — Per-turn agent state surfaced in the data context. Top-level keys in the data context are objects, not primitives, so anything an agent wants to expose to templates or downstream consumers rides under this single record.
- [AgentStateExtensions](AgentStateExtensions.md) — Convenience accessors for pulling the active [AgentState](AgentState.md) off an [IDataContextScope](../Common/IDataContextScope.md) without callers having to remember the well-known key, plus per-field shortcuts that delegate to the same lookup.
- [AgentToolConfig](AgentToolConfig.md) — Per-agent tool-loop guardrails.
- [IAgent](IAgent.md) — An autonomous component that ingests input turns, drives a model, and produces output turns. Identity, heartbeat cadence, channels, and conversation state are internal and reachable through the services that own the agent (config provider, context store, message bus).
- [IAgentConfigProvider](IAgentConfigProvider.md) — Source of truth for agent configuration. Reads from the configured agents directory (`<Data>/agents/<id>.json`) and is the single read API for both "what agents exist" and "what's the config for this agent". Implementations may cache by file metadata but must reflect on-disk edits without a host restart.
- [IAgentIterationRunner](IAgentIterationRunner.md) — Runs a single agent iteration: builds the prompt from the supplied context and turn batch, invokes the language model (with the empty-response retry), persists the model's output via the active context store, and returns any tool-result turns the caller should feed back on the next iteration. Knows nothing about session queues, agent locks, or interrupt subscriptions — those concerns belong to the surrounding loop owner.
- [IAgentLock](IAgentLock.md) — Scoped passthrough over [IAgentLockManager](IAgentLockManager.md) that resolves the agent id from the ambient data scope, so callers don't have to thread the id through every acquire site. Use this from inside an agent-owned scope; resolve [IAgentLockManager](IAgentLockManager.md) directly when you need to lock for an arbitrary agent.
- [IAgentLockManager](IAgentLockManager.md) — Hands out per-agent exclusive locks. Each agent id is backed by a dedicated single-permit semaphore, so concurrent acquires for the same agent serialize; acquires for different agents proceed independently. Implementations are expected to be singleton.
- [IAgentService](IAgentService.md) — A service whose lifetime is tied to a single agent: it runs on agent startup and stays alive for the life of that agent. Useful for event handlers, background tasks, or anything that needs the agent's scope to be live when it begins work.
- [IAgentStateTracker](IAgentStateTracker.md) — Writes the active [AgentState](AgentState.md) into the current data context scope. Centralizes the construction so every caller stamps the same shape (channel, event id, correlation id).
- [IAgentTokenStore](IAgentTokenStore.md) — In-process store that issues opaque single-use bearer tokens bound to an [AgentInfo](AgentInfo.md). Tokens are valid until consumed (via [IAgentTokenStore](IAgentTokenStore.md).`TryGetAgentInformation`) or until they expire — whichever comes first.
- [ILockScope](ILockScope.md) — Disposable handle to an acquired agent lock. The scope owns the underlying semaphore permit; disposing it releases the permit and flips [ILockScope](ILockScope.md).`Active` to `false`. If the holder forgets to dispose, the finalizer still releases the permit so the lock isn't wedged for the rest of the process lifetime.
- [IterationOutcome](IterationOutcome.md) — Result of running one agent iteration: was the turn interrupted before completion, and any tool-result turns the inference produced that the caller should feed back into its driver on the next iteration.
- [SaveAgentConfigResult](SaveAgentConfigResult.md) — Outcome of an [IAgentConfigProvider](IAgentConfigProvider.md).`SaveAsync` attempt. Pattern-match to handle each branch.
- [SystemTick](SystemTick.md) — Periodic host heartbeat broadcast onto the event bus. Subscribers use it as a coarse "wall-clock advanced" signal — agent idle detection, refreshes, scheduled chores — without each component running its own timer.

