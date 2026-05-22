# LlamaShears.Core.Abstractions.Agent.ITransientAgentFactory

Assembly: `LlamaShears.Core.Abstractions`

Creates a transient agent rooted as a child of the caller's current
session. Resolves the parent [SessionPath](Sessions/SessionPath.md) from
the ambient data context scope and stamps the supplied initial turn
onto the new agent's data so it boots straight into work.

## Methods

### `CreateTransientAgent`([AgentConfig](AgentConfig.md) config, string name, [ModelTurn](../Provider/ModelTurn.md) initialTurn, IEnumerable<KeyValuePair<string, object>> data, CancellationToken cancellationToken)

Builds an [AgentHandle](../../AgentHandle.md) for an [ITransientAgent](ITransientAgent.md)
running under `config`. The new session is a
child of the caller's session with `name` as the
channel suffix. `initialTurn` is appended to
`data` under [TransientAgentInitialPrompt](TransientAgentInitialPrompt.md).`DataKey`.

#### Parameters

- `config` — Agent config for the transient agent.
- `name` — Session channel name; must be non-empty.
- `initialTurn` — First turn the transient agent will see; must have [ModelTurn](../Provider/ModelTurn.md).`Role` = [ModelRole](../Provider/ModelRole.md).`User`.
- `data` — Additional data scope entries to seed the child.
- `cancellationToken` — Cancellation for the underlying build pipeline.

