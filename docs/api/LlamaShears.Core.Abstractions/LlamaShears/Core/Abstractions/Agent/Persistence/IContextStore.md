# LlamaShears.Core.Abstractions.Agent.Persistence.IContextStore

Assembly: `LlamaShears.Core.Abstractions`

Storage seam for per-agent conversation logs. Implementations decide
the backing format (today: append-only JSON-line files on disk) and
expose live [IAgentContext](IAgentContext.md) views plus archive-reading,
listing, and clearing operations.

## Remarks

The session dimension rides on [SessionId](../Sessions/SessionId.md). The default
session is identified by [SessionId](../Sessions/SessionId.md).`IsDefault` and
resolves to the agent's root storage layout; non-default sessions
persist under `<agentId>/<sessionName>/` with their
current file named `<sessionId:n>.json`.

## Methods

### `ClearAsync`([SessionId](../Sessions/SessionId.md) session, bool archive, CancellationToken cancellationToken)

Clears the session's stored context. With
`archive`=true, renames the session's current
file to `<UnixMillis>.json`; otherwise deletes it. The
agent or session folder is never removed by the framework — that
is the user's or a plugin's job.

### `DeleteAsync`([ArchiveId](ArchiveId.md) archiveId, CancellationToken cancellationToken)

Deletes a single archive file identified by `archiveId`.
Does not touch the session's current file, other archives, or the
agent folder.

### `ListAgentsAsync`(CancellationToken cancellationToken)

Returns the ids of every agent that has any persisted state in
this store, in stable lexicographic order.

### `ListArchivesAsync`([SessionId](../Sessions/SessionId.md) session, CancellationToken cancellationToken)

Returns every archive id stored for `session` in
chronological order (oldest first). The session's current,
non-archived context is not included.

### `OpenAsync`([SessionId](../Sessions/SessionId.md) session, CancellationToken cancellationToken)

Opens the live, mutable context for `session`,
loading any persisted entries on first open. Repeated calls for
the same session return the same instance for the lifetime of the
store.

### `ReadArchiveAsync`([ArchiveId](ArchiveId.md) archiveId, CancellationToken cancellationToken)

Streams the persisted entries from a specific archived context
file identified by `archiveId`.

### `ReadCurrentAsync`([SessionId](../Sessions/SessionId.md) session, CancellationToken cancellationToken)

Streams the persisted entries from the session's current (active)
context file in arrival order. Does not affect any open
[IAgentContext](IAgentContext.md).

