# LlamaShears.Core.Abstractions.Agent.ITransientAgentFactory

Assembly: `LlamaShears.Core.Abstractions`

Creates a transient agent rooted as a child of the caller's current
session. Resolves the parent [SessionPath](Sessions/SessionPath.md) from
the ambient data context scope and stamps the supplied initial turn
onto the new agent's data so it boots straight into work.

## Methods

### `CreateTransientAgent`([AgentConfig](AgentConfig.md) config, [SessionId](Sessions/SessionId.md) session, [ModelTurn](../Provider/ModelTurn.md) initialTurn, IEnumerable<KeyValuePair<string, object>> data, CancellationToken cancellationToken)

Builds an [AgentHandle](../../AgentHandle.md) for an [ITransientAgent](ITransientAgent.md)
running under `config`. The new session is a
child of the caller's session and keeps`session`'s canonical id (including its
[SessionId](Sessions/SessionId.md).`Id`). Callers that subscribe to
`agent:idle` / `agent:turn` before spawn depend on
that identity. `initialTurn` is appended to
`data` under [TransientAgentInitialPrompt](TransientAgentInitialPrompt.md).`DataKey`.

#### Parameters

- `config` — Agent config for the transient agent.
- `session` — Child session id. Must not be the default session; [SessionId](Sessions/SessionId.md).`AgentId` must match `config`.
- `initialTurn` — First turn the transient agent will see; must have [ModelTurn](../Provider/ModelTurn.md).`Role` = [ModelRole](../Provider/ModelRole.md).`User`.
- `data` — Additional data scope entries to seed the child.
- `cancellationToken` — Cancellation for the underlying build pipeline.

