# LlamaShears.Core.Abstractions.Agent.Persistence.IAgentContext

Assembly: `LlamaShears.Core.Abstractions`

Live, mutable view of one agent session's persisted conversation log.
Backed by an [IContextStore](IContextStore.md); appending appends both
in-memory and to durable storage. Snapshots of [IAgentContext](IAgentContext.md).`Turns`
and [IAgentContext](IAgentContext.md).`Entries` are stable at the moment of access.

## Properties

### `AgentId`

Identifier of the agent whose log this represents.

### `Entries`

Snapshot of every persisted entry — turns and any future
non-turn entry types — in arrival order.

### `SessionId`

Session id this context belongs to; `null` for the
agent's default (main) session. Non-default sessions persist under
a per-session subfolder rather than the agent root.

### `TokenCount`

Last observed cumulative model token count for the conversation,
taken from the most recent [ModelTokenInformationContextEntry](../../Provider/ModelTokenInformationContextEntry.md)
in [IAgentContext](IAgentContext.md).`Entries`. Zero when no completion has been recorded yet.

### `Turns`

Snapshot of the conversation as [ModelTurn](../../Provider/ModelTurn.md) values,
filtered out of the polymorphic entry log. Stable for the duration
of the call.

## Methods

### `AppendAsync`([IContextEntry](../../Provider/IContextEntry.md) entry, CancellationToken cancellationToken)

Appends `entry` to the live log and to the
underlying store atomically. Subsequent reads of
[IAgentContext](IAgentContext.md).`Turns` / [IAgentContext](IAgentContext.md).`Entries` include it.

## Events

### `Appended`

Raised after [IAgentContext](IAgentContext.md).`AppendAsync` has committed an entry to
both durable storage and the in-memory snapshot. Subscribers can
rely on the entry being visible from [IAgentContext](IAgentContext.md).`Entries` /
[IAgentContext](IAgentContext.md).`Turns` by the time the event fires.

### `Cleared`

Raised when the context is cleared in-memory (typically following
[IContextStore](IContextStore.md).`ClearAsync`).
Subscribers should treat previously-observed entries as discarded.

