# LlamaShears.Core.Abstractions.Agent.Sessions.EphemeralSessionReference

Assembly: `LlamaShears.Core.Abstractions`

Stable handle naming a target session — the
([EphemeralSessionReference](EphemeralSessionReference.md).`AgentId`, [EphemeralSessionReference](EphemeralSessionReference.md).`SessionId`) pair used by an
ephemeral child session to address its parent (e.g. for routing the
reply published via `session_reply`).

## Parameters

- `AgentId` — Owning agent id.
- `SessionId` — Session id within the agent; `null` identifies the
agent's default (main) session.

## Properties

### `AgentId`

Owning agent id.

### `SessionId`

Session id within the agent; `null` identifies the
agent's default (main) session.

## Methods

### `EphemeralSessionReference`(string AgentId, Nullable<Guid> SessionId)

Stable handle naming a target session — the
([EphemeralSessionReference](EphemeralSessionReference.md).`AgentId`, [EphemeralSessionReference](EphemeralSessionReference.md).`SessionId`) pair used by an
ephemeral child session to address its parent (e.g. for routing the
reply published via `session_reply`).

#### Parameters

- `AgentId` — Owning agent id.
- `SessionId` — Session id within the agent; `null` identifies the
agent's default (main) session.

