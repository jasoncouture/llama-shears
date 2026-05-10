# Memory and RAG

Long-term memory for an agent is a directory of markdown files plus a derived vector index. Files are the source of truth; the index is a search-time accelerator that the framework keeps in sync. The contracts are in [`Core.Abstractions.Memory`](../../src/public/LlamaShears.Core.Abstractions.Memory/) (`IMemoryStore`, `IMemorySearcher`, `IMemoryIndexer`); the implementation is [`SqliteMemoryService`](../../src/LlamaShears.Core/Memory/SqliteMemoryService.cs); the periodic reconciliation runs in [`MemoryIndexerBackgroundService`](../../src/LlamaShears.Core/Memory/MemoryIndexerBackgroundService.cs).

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
    └── .memory.db          # SQLite + sqlite-vec; framework-owned
```

- Memory files are markdown, written under `memory/YYYY-MM-DD/<unix-seconds>.md`. Same-second collisions get suffixed (`-1`, `-2`, …).
- The directory layout is the framework's responsibility; agents should call `memory_store` rather than write files by hand. The format is plain text — there's no front-matter or required structure inside the file.
- The vector index lives at `<workspace>/system/.memory.db`. It's a SQLite database with the `sqlite-vec` extension loaded, accessed through `Microsoft.Extensions.VectorData` via the SemanticKernel SqliteVec connector. The collection is named `memories` and each row is a `MemoryVectorRecord(Path, Hash, Vector)` — `Path` is the SHA-keyed primary key (relative to the workspace), `Hash` is the SHA-256 of the file contents (lowercase hex), `Vector` is the embedding stored in sqlite-vec's binary form.

The on-disk shape was chosen so that an agent never has to manage the index. The agent does file operations — write, edit, delete — and the framework reconciles.

## Three operations

### `IMemoryStore.StoreAsync(agentId, content, cancellationToken)`

1. Resolve the agent's workspace and embedding model.
2. Pick a path under `memory/YYYY-MM-DD/<unix-seconds>.md`, create the directory if needed, write the file (UTF-8).
3. Compute SHA-256 of the content (lowercase hex).
4. Embed the content (with the configured document prefix) and `UpsertAsync` `(Path, Hash, Vector)` into the `memories` collection.
5. Return a `MemoryRef(relativePath)`.

If indexing fails (`VectorStoreException`, `InvalidOperationException`, network failure on the embedder), the file write still wins — the file is on disk and the next reconciliation pass will catch it. Indexing failures are logged at `Warning`.

### `IMemorySearcher.SearchAsync(agentId, query, limit, minScore, cancellationToken)`

1. Resolve workspace + embedding model.
2. If `<workspace>/system/.memory.db` doesn't exist, return `[]`.
3. Embed the query (with the configured query prefix).
4. Open the collection (creating the database with the right vector dimension if it doesn't yet exist).
5. Call `collection.SearchAsync(queryVector, top: max(limit * 4, limit))`. sqlite-vec ranks by cosine *distance* (lower is better; 0 = identical); the service translates it to the similarity scale callers expect (`1.0 - distance`, where `1.0` is identical).
6. For each hit at or above `minScore`, check that the file still exists on disk. If it doesn't, drop the hit (the next reconcile will GC the index entry).
7. Sort descending by similarity, truncate to `limit`, return `MemorySearchResult(relativePath, score)` records.

Step 6 is the **retrieval-time self-healing**: orphaned index entries never make it back to the model. The model sees only hits whose files are still there.

`Agent.SearchMemoriesAsync` (the agent loop's caller) reads the file contents for each hit and surfaces them into the per-turn ephemeral block — see [agent-loop.md](agent-loop.md), step 3, and [prompt-context.md](prompt-context.md). Defaults: `limit = 5`, `minScore = 0.30`. Matched memories are injected with their first-line summary rather than full body, to keep the per-turn block tight; the agent can still reach the full content via the file path.

### `IMemoryIndexer.ReconcileAsync(agentId, force, cancellationToken)`

The periodic / on-demand sync. For one agent:

1. Snapshot every `(Path, Hash)` row currently in the index.
2. Walk `<workspace>/memory/**/*.md` on disk.
   - Compute the file's SHA-256.
   - If the row is missing → embed and upsert. (`added++`)
   - If the row's hash differs from disk → re-embed and upsert. (`updated++`)
   - If the hashes match and `force` is false → skip.
   - If `force` is true → re-embed and upsert regardless. (`updated++`)
3. After the walk, every index row whose `Path` was *not* visited is an orphan. Delete each. (`removed++`)
4. Return a `MemoryReconciliation(Added, Updated, Removed, Total)` (Total = files seen on disk).

Reconciliation is per-agent. There's no global "rebuild everything" path — the background service iterates agents.

## Vector-dimension auto-rebuild

If the configured embedding model's vector dimension doesn't match the dimension already baked into the existing `.memory.db` (because the operator changed embedding models, or because a new model has a different output size), `SqliteMemoryService` detects the mismatch when opening the collection and rebuilds the index from scratch — drops the existing collection, recreates it with the new dimension, and lets the next reconcile pass refill it from the source-of-truth markdown files. The reconcile is idempotent and can recover the entire corpus from disk, so a rebuild costs only re-embedding time.

## Periodic reconciliation

`MemoryIndexerBackgroundService` is a hosted service that runs reconciliation on a configured interval:

- **`Memory:Indexer:Enabled`** (default `true`) — toggles the service.
- **`Memory:Indexer:Interval`** (default `00:30:00`) — gap between full scans. Implemented with `Task.Delay`, so a slow scan doesn't queue ticks behind it.
- **`Memory:Indexer:ForceOnStartup`** (default `false`) — on the *first* scan after process start, pass `force: true`. Useful when the embedding model has changed and previously-cached vectors need a refresh.

The service iterates `IAgentConfigProvider.ListAgentIds()` on each scan; agents that come and go between scans naturally pick up reconciliation on the next pass.

## Embedding configuration

Two layers, with per-agent overriding host:

- **Per-agent** (`AgentConfig.Embedding`) — a full `ModelConfiguration` (`id`, `think`, `contextLength`, `tokenLimit`, plus any free-form keys like `queryPrefix`, `documentPrefix`, `keepAlive`). All unrecognized keys land in `ModelConfiguration.Parameters` and ride through to the provider. Optional; if absent, the host defaults are used.
- **Host-level** (`Memory:DefaultEmbedding*` in `appsettings.json`) supplies the fallback `id` and prefix strings; provider-specific knobs (e.g. Ollama's `keepAlive`) live on the provider's own host options:

  ```json
  "Memory": {
    "DefaultEmbeddingModel": "OLLAMA/embeddinggemma:latest",
    "DefaultEmbeddingQueryPrefix": "task: search result | query: ",
    "DefaultEmbeddingDocumentPrefix": "title: none | text: "
  },
  "Providers": {
    "Ollama": { "KeepAlive": "01:00:00" }
  }
  ```

The prefixes are part of the `embeddinggemma` task convention — see the model card. They're configurable so other embedders that use different prefixes can drop in without code changes.

Two embedding providers ship today:

- **Ollama** (`OLLAMA/<model>`) — calls a configured Ollama endpoint. The default for backward compat with the original setup.
- **ONNX in-process** (`ONNX/<model>`) — runs the embedder inside the host process via `OnnxRuntime`, using a per-model layout under the `Providers:Onnx:Embeddings:Models` config tree. Currently scoped to all-MiniLM-family sentence-transformers checkpoints; pooling strategy and max sequence length come from per-model config.

If neither the agent nor the host provides an embedding model, memory operations throw `InvalidOperationException` with the message `"Agent '<id>' has no embedding model and no host-level default is configured."` This is loud-failure behavior on purpose: silently disabling RAG would mask a configuration mistake.

## SQLite connection pooling

`Microsoft.Data.Sqlite`'s default connection pooling lets handles outlive the calling scope. `SqliteMemoryService` opens connections with pooling **disabled** — pooling caused live handles from one agent to be torn down when another agent's `ResetIndex` ran on the same path. Without pooling, each call gets a fresh connection that's closed deterministically, and cross-agent reconciles can't step on each other.

## Agent-facing tools

Three MCP tools live behind the host's own listener (registered in [`ModelContextProtocolServiceCollectionExtensions`](../../src/LlamaShears.Api/Tools/ModelContextProtocol/ModelContextProtocolServiceCollectionExtensions.cs)). Names follow the `<category>_<action>` convention:

- **`memory_store`** — calls `IMemoryStore.StoreAsync`. Returns the relative path of the new file.
- **`memory_search`** — calls `IMemorySearcher.SearchAsync`. Returns scored relative paths plus content.
- **`memory_index`** — calls `IMemoryIndexer.ReconcileAsync(force: true)` for the calling agent. Returns the reconciliation summary (added / updated / removed / total).

`memory_index` exists primarily as an escape hatch for "I edited something out-of-band and don't want to wait for the next reconciliation tick."

## What's deliberately not here

- **No semantic deduplication.** Two memories that say the same thing in different words land as two files. The agent is free to consolidate when it wants to.
- **No versioning.** Edits in place change the hash; the new version replaces the old vector. If you want history, write to a new file.
- **No cross-agent sharing.** Each agent's `memory/` is private to its workspace. Sharing memory across agents is a workspace-layout question (see [agent-workspace.md](agent-workspace.md), open items), not a memory-system question.
- **No relevance tuning.** The 0.30 score floor and the 5-result limit live in `Agent.cs` constants; they're not currently configurable per-agent. They're exposed as tunable knobs only when there's a concrete reason to tune them.
