# LlamaShears.Core.Abstractions.Memory.IMemoryStore

Assembly: `LlamaShears.Core.Abstractions.Memory`

Writes a new memory file under the agent's workspace. Storage is the
source of truth; the embedding index is derivative and self-heals on
retrieval and on full reconcile (see [IMemoryIndexer](IMemoryIndexer.md)).

## Methods

### `StoreAsync`(string agentId, string content, CancellationToken cancellationToken)

Persists `content` under
`memory/YYYY-MM-DD/<unix-seconds>.md` in the agent's
workspace and triggers eager indexing. Indexing failures are
logged and swallowed — the file is still written, and the next
[IMemoryIndexer](IMemoryIndexer.md).`ReconcileAsync` picks it up.

