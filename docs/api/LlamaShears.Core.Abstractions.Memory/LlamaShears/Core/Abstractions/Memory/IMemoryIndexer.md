# LlamaShears.Core.Abstractions.Memory.IMemoryIndexer

Assembly: `LlamaShears.Core.Abstractions.Memory`

Reconciles the agent's memory index against the filesystem.
Walks `memory/**/*.md` and the index together: new files get
indexed, changed files re-indexed, and orphaned index entries
removed. Invoked explicitly by the `memory_index` tool and on
any future periodic schedule.

## Methods

### `ReconcileAsync`(string agentId, bool force, CancellationToken cancellationToken)

Walks the agent's `memory/**/*.md` tree and the index in
lockstep, applying inserts, updates, and deletions so the index
matches the filesystem. Returns counts for telemetry. When
`force` is `true`, every file is re-embedded
regardless of whether its content hash already matches the indexed
hash — use this after changing embedding-model or prompt-decoration
behavior so the existing index can be rebuilt without editing or
re-saving each file.

