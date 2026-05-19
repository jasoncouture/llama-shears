# LlamaShears.Core.Abstractions.Agent.Persistence.IContextStore

Assembly: `LlamaShears.Core.Abstractions`

Storage seam for per-agent conversation logs. Implementations decide
the backing format (today: append-only JSON-line files on disk) and
expose live [IAgentContext](IAgentContext.md) views plus archive-reading,
listing, and clearing operations.

## Remarks

Methods take a nullable `sessionId` dimension; the default
session is identified by `null` and resolves to the
agent's root storage layout (backward-compatible). Non-default
sessions persist under a per-session subfolder. The store does not
auto-load or otherwise lifecycle-manage non-default sessions — they
exist only when something explicitly opens them.

## Methods

### `ClearAsync`(string agentId, bool archive, CancellationToken cancellationToken)

Convenience overload that targets the agent's default session.

### `ClearAsync`(string agentId, Nullable<Guid> sessionId, bool archive, CancellationToken cancellationToken)

Clears the session's stored context. With
`archive`=true, renames `current.json` to
`<UnixMillis>.json`; otherwise deletes
`current.json`. The agent or session folder is never removed
by the framework — that is the user's or a plugin's job.

### `DeleteAsync`([ArchiveId](ArchiveId.md) archiveId, CancellationToken cancellationToken)

Deletes a single archive file identified by `archiveId`.
Does not touch `current.json`, other archives, or the agent folder.

### `ListAgentsAsync`(CancellationToken cancellationToken)

Returns the ids of every agent that has any persisted state in
this store, in stable lexicographic order. GUID-named subfolders
(non-default session storage) are not surfaced as agents.

### `ListArchivesAsync`(string agentId, Nullable<Guid> sessionId, CancellationToken cancellationToken)

Returns every archive id stored for
(`agentId`, `sessionId`)
in chronological order (oldest first). The session's current,
non-archived context is not included.

### `ListArchivesAsync`(string agentId, CancellationToken cancellationToken)

Convenience overload that targets the agent's default session.

### `OpenAsync`(string agentId, Nullable<Guid> sessionId, CancellationToken cancellationToken)

Opens the live, mutable context for
(`agentId`, `sessionId`),
loading any persisted entries on first open. Repeated calls for
the same pair return the same instance for the lifetime of the
store.

### `OpenAsync`(string agentId, CancellationToken cancellationToken)

Convenience overload that targets the agent's default session.

### `ReadArchiveAsync`([ArchiveId](ArchiveId.md) archiveId, CancellationToken cancellationToken)

Streams the persisted entries from a specific archived context
file identified by `archiveId`.

### `ReadCurrentAsync`(string agentId, Nullable<Guid> sessionId, CancellationToken cancellationToken)

Streams the persisted entries from the session's current (active)
context file in arrival order. Does not affect any open
[IAgentContext](IAgentContext.md).

### `ReadCurrentAsync`(string agentId, CancellationToken cancellationToken)

Convenience overload that targets the agent's default session.

