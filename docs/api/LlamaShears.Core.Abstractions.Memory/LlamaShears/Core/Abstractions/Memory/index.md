# LlamaShears.Core.Abstractions.Memory

## Types

- [IMemoryIndexer](IMemoryIndexer.md) — Reconciles the agent's memory index against the filesystem. Walks `memory/**/*.md` and the index together: new files get indexed, changed files re-indexed, and orphaned index entries removed. Invoked explicitly by the `memory_index` tool and on any future periodic schedule.
- [IMemorySearcher](IMemorySearcher.md) — Vector-search over the agent's memory index. Returns workspace-relative paths and similarity scores; the agent reads bodies on demand via the filesystem read-file tool.
- [IMemoryStore](IMemoryStore.md) — Writes a new memory file under the agent's workspace. Storage is the source of truth; the embedding index is derivative and self-heals on retrieval and on full reconcile (see [IMemoryIndexer](IMemoryIndexer.md)).
- [MemoryReconciliation](MemoryReconciliation.md) — Counts returned by [IMemoryIndexer](IMemoryIndexer.md).`ReconcileAsync`. Pure telemetry — the source of truth for memory content is the filesystem, the index is derivative.
- [MemoryRef](MemoryRef.md) — Lightweight reference to a memory file written via [IMemoryStore](IMemoryStore.md).`StoreAsync`. Workspace-relative path only — the agent reads the body on demand.
- [MemorySearchResult](MemorySearchResult.md) — One hit returned by [IMemorySearcher](IMemorySearcher.md).`SearchAsync`: where the memory lives, how similar it is to the query, the first line as a one-shot summary, and the full body. Both the summary and the body come from a single cached file read so callers don't need to re-open the file.

