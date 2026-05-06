# Memory and RAG

Long-term memory for an agent is a directory of markdown files plus a derived vector index. Files are the source of truth; the index is a search-time accelerator that the framework keeps in sync. The contracts are in [`Core.Abstractions/Memory/`](../../src/LlamaShears.Core.Abstractions/Memory/) (`IMemoryStore`, `IMemorySearcher`, `IMemoryIndexer`); the implementation is [`SqliteMemoryService`](../../src/LlamaShears.Core/Memory/SqliteMemoryService.cs); the periodic reconciliation runs in [`MemoryIndexerBackgroundService`](../../src/LlamaShears.Core/Memory/MemoryIndexerBackgroundService.cs).

## On disk

```
<workspace>/
├── memory/
│   ├── 2026-05-05/
│   │   ├── 1714900000.md
│   │   └── 1714900142.md
│   └── 2026-05-06/
│       └── ...
└── system/
    └── .memory.db          # SQLite; framework-owned
```

- Memory files are markdown, written under `memory/YYYY-MM-DD/<unix-seconds>.md`. Same-second collisions get suffixed (`-1`, `-2`, …).
- The directory layout is the framework's responsibility; agents should call `memory_store` rather than write files by hand. The format is plain text — there's no front-matter or required structure inside the file.
- The SQLite index lives at `<workspace>/system/.memory.db`. Schema is one table, `memories(path TEXT PRIMARY KEY, hash TEXT NOT NULL, vector BLOB NOT NULL)`. Vectors are stored as raw little-endian `float32`. Cosine similarity is computed in C# at query time — there's no SQLite extension at play yet.

The on-disk shape was chosen so that an agent never has to manage the index. The agent does file operations — write, edit, delete — and the framework reconciles.

## Three operations

### `IMemoryStore.StoreAsync(agentId, content, ct)`

1. Resolve the agent's workspace and embedding model.
2. Pick a path under `memory/YYYY-MM-DD/<unix-seconds>.md`, create the directory if needed, write the file (UTF-8).
3. Compute SHA-256 of the content (lowercase hex).
4. Embed the content (with the configured document prefix) and upsert `(path, hash, vector)` into the index.
5. Return a `MemoryRef(relativePath)`.

If indexing fails (SQLite error, embedding model unreachable), the file write still wins — the file is on disk and the next reconciliation pass will catch it. Indexing failures are logged at `Warning`.

### `IMemorySearcher.SearchAsync(agentId, query, limit, minScore, ct)`

1. Resolve workspace + embedding model.
2. If `<workspace>/system/.memory.db` doesn't exist, return `[]`.
3. If the table is empty, return `[]` without embedding the query (no point doing the round-trip).
4. Embed the query (with the configured query prefix).
5. Read every `(path, vector)` row, compute cosine similarity in C#.
6. For each hit at or above `minScore`, check that the file still exists on disk. If it doesn't, drop the hit (the next reconcile will GC the index entry).
7. Sort descending by score, truncate to `limit`, return `MemorySearchResult(relativePath, score)` records.

Steps 6 is the **retrieval-time self-healing**: orphaned index entries never make it back to the model. The model sees only hits whose files are still there.

`Agent.SearchMemoriesAsync` (the agent loop's caller) reads the file contents for each hit and surfaces `(relativePath, content, score)` into the per-turn ephemeral block — see [agent-loop.md](agent-loop.md), step 3, and [prompt-context.md](prompt-context.md). Defaults: `limit = 5`, `minScore = 0.30`.

### `IMemoryIndexer.ReconcileAsync(agentId, force, ct)`

The periodic / on-demand sync. For one agent:

1. Snapshot every `(path, hash)` row currently in the index.
2. Walk `<workspace>/memory/**/*.md` on disk.
   - Compute the file's SHA-256.
   - If the row is missing → embed and insert. (`added++`)
   - If the row's hash differs from disk → re-embed and update. (`updated++`)
   - If the hashes match and `force` is false → skip.
   - If `force` is true → re-embed and update regardless. (`updated++`)
3. After the walk, every index row whose `path` was *not* visited is an orphan. Delete each. (`removed++`)
4. Return a `MemoryReconciliation(Added, Updated, Removed, Total)` (Total = files seen on disk).

Reconciliation is per-agent. There's no global "rebuild everything" path — the background service iterates agents.

## Periodic reconciliation

`MemoryIndexerBackgroundService` is a hosted service that runs reconciliation on a configured interval:

- **`Memory:Indexer:Enabled`** (default `true`) — toggles the service.
- **`Memory:Indexer:Interval`** (default `00:30:00`) — gap between full scans. Implemented with `Task.Delay`, so a slow scan doesn't queue ticks behind it.
- **`Memory:Indexer:ForceOnStartup`** (default `false`) — on the *first* scan after process start, pass `force: true`. Useful when the embedding model has changed and previously-cached vectors need a refresh.

The service iterates `IAgentConfigProvider.ListAgentIds()` on each scan; agents that come and go between scans naturally pick up reconciliation on the next pass.

## Embedding configuration

Two layers, with per-agent overriding host:

- **Per-agent** (`AgentConfig.Embedding`) — `id` (provider/model), `keepAlive`, `queryPrefix`, `documentPrefix`. Optional; if absent, the host defaults are used.
- **Host-level** (`Memory:DefaultEmbedding*` in `appsettings.json`):

  ```json
  "Memory": {
    "DefaultEmbeddingModel": "OLLAMA/embeddinggemma:latest",
    "DefaultEmbeddingQueryPrefix": "task: search result | query: ",
    "DefaultEmbeddingDocumentPrefix": "title: none | text: ",
    "DefaultEmbeddingKeepAlive": "01:00:00"
  }
  ```

The prefixes are part of the `embeddinggemma` task convention — see the model card. They're configurable so other embedders that use different prefixes can drop in without code changes.

If neither the agent nor the host provides an embedding model, memory operations throw `InvalidOperationException` with the message `"Agent '<id>' has no embedding model and no host-level default is configured."` This is loud-failure behavior on purpose: silently disabling RAG would mask a configuration mistake.

## Why raw SQLite (and not `Microsoft.Extensions.VectorData`)

The shared memory index records the long-running plan: vector storage will move to `Microsoft.Extensions.VectorData` with the sqlite-vec connector. That hasn't happened. Today the schema is hand-written, the similarity calculation is in C#, and the `.memory.db` file is a regular SQLite database with no extensions loaded.

The current shape was chosen for two reasons:

1. **No native dependency.** sqlite-vec ships as a native loadable extension; vendoring it cleanly across Linux/macOS/Windows for the developer build wasn't worth the complexity yet.
2. **The query path is dominated by embedding latency, not similarity math.** Once you've paid for the embedding round-trip, walking 200 vectors in C# is a rounding error. When the corpus grows past the point where C# scan dominates, that's the trigger to migrate.

The migration plan is a same-shape replacement: same schema (path, hash, vector), same self-healing semantics, same agent-facing API. Only the similarity computation moves into the database.

## Agent-facing tools

Three MCP tools live behind the host's own listener (registered in [`ModelContextProtocolServiceCollectionExtensions`](../../src/LlamaShears.Api/Tools/ModelContextProtocol/ModelContextProtocolServiceCollectionExtensions.cs)):

- **`memory_store`** — calls `IMemoryStore.StoreAsync`. Returns the relative path of the new file.
- **`memory_search`** — calls `IMemorySearcher.SearchAsync`. Returns scored relative paths plus content.
- **`memory_index`** — calls `IMemoryIndexer.ReconcileAsync(force: true)` for the calling agent. Returns the reconciliation summary (added / updated / removed / total).

`memory_index` exists primarily as an escape hatch for "I edited something out-of-band and don't want to wait for the next reconciliation tick."

## What's deliberately not here

- **No semantic deduplication.** Two memories that say the same thing in different words land as two files. The agent is free to consolidate when it wants to.
- **No versioning.** Edits in place change the hash; the new version replaces the old vector. If you want history, write to a new file.
- **No cross-agent sharing.** Each agent's `memory/` is private to its workspace. Sharing memory across agents is a workspace-layout question (see [agent-workspace.md](agent-workspace.md), open items), not a memory-system question.
- **No relevance tuning.** The 0.30 score floor and the 5-result limit live in `Agent.cs` constants; they're not currently configurable per-agent. They're exposed as tunable knobs only when there's a concrete reason to tune them.
