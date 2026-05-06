# LlamaShears.Core.Abstractions.Agent.Persistence.IContextStore

Assembly: `LlamaShears.Core.Abstractions`

Storage seam for per-agent conversation logs. Implementations decide
the backing format (today: append-only JSON-line files on disk) and
expose live [IAgentContext](IAgentContext.md) views plus archive-reading,
listing, and clearing operations.

## Methods

### `ClearAsync`(string agentId, bool archive, CancellationToken cancellationToken)

Clears the agent's stored context. With `archive`=true,
renames `current.json` to `<UnixMillis>.json`; otherwise
deletes `current.json`. The agent's folder is never removed by the
framework — that is the user's or a plugin's job.

### `DeleteAsync`([ArchiveId](ArchiveId.md) archiveId, CancellationToken cancellationToken)

Deletes a single archive file identified by `archiveId`.
Does not touch `current.json`, other archives, or the agent folder.

### `ListAgentsAsync`(CancellationToken cancellationToken)

Returns the ids of every agent that has any persisted state in
this store, in stable lexicographic order.

### `ListArchivesAsync`(string agentId, CancellationToken cancellationToken)

Returns every archive id stored for `agentId`
in chronological order (oldest first). The agent's current,
non-archived context is not included.

### `OpenAsync`(string agentId, CancellationToken cancellationToken)

Opens the live, mutable context for `agentId`,
loading any persisted entries on first open. Repeated calls for
the same agent return the same instance for the lifetime of the
store.

### `ReadArchiveAsync`([ArchiveId](ArchiveId.md) archiveId, CancellationToken cancellationToken)

Streams the persisted entries from a specific archived context
file identified by `archiveId`.

### `ReadCurrentAsync`(string agentId, CancellationToken cancellationToken)

Streams the persisted entries from the agent's current (active)
context file in arrival order. Does not affect any open
[IAgentContext](IAgentContext.md).

