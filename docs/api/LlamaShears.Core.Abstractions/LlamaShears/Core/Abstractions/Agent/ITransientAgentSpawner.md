# LlamaShears.Core.Abstractions.Agent.ITransientAgentSpawner

Assembly: `LlamaShears.Core.Abstractions`

Spawns a transient sub-agent rooted under the caller's session and returns
the new child [SessionId](Sessions/SessionId.md) on success.

## Methods

### `SpawnAgentAsync`([SessionId](Sessions/SessionId.md) sessionId, [AgentConfig](AgentConfig.md) config, IEnumerable<[ModelTurn](../Provider/ModelTurn.md)> turns, CancellationToken cancellationToken)

Creates a transient child agent under `sessionId` and
seeds its context with the supplied turns.

#### Parameters

- `sessionId` — Parent session id.
- `config` — Agent config the child runs under.
- `turns` — Initial turns to seed the child's context.
- `cancellationToken` — Cancellation token.

#### Returns

The new child [SessionId](Sessions/SessionId.md).

