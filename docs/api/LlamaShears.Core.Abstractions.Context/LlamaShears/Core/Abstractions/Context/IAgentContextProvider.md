# LlamaShears.Core.Abstractions.Context.IAgentContextProvider

Assembly: `LlamaShears.Core.Abstractions.Context`

Composes [AgentContext](AgentContext.md) snapshots on demand from the
host's authoritative sources (config, language model, plugins, etc.).
Returns `null` when no context can be built — for the
parameterless overload, when there is no ambient agent; for the
id-bearing overload, when the agent does not exist.

## Methods

### `CreateAgentContextAsync`(string agentId, CancellationToken cancellationToken)

Builds a snapshot for `agentId`. Returns
`null` when no agent with that id is configured.

### `CreateAgentContextAsync`(CancellationToken cancellationToken)

Builds a snapshot for the ambient agent — the agent whose
execution scope the calling code is running inside. Returns
`null` when there is no current ambient agent.

