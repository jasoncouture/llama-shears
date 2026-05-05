---
name: Vector store choice — Microsoft.Extensions.VectorData + SqliteVec
description: LlamaShears uses Microsoft.Extensions.VectorData with the sqlite-vec connector for memory storage; landed 2026-05-05
type: project
---

LlamaShears uses **Microsoft.Extensions.VectorData** as the vector-store abstraction with **`Microsoft.SemanticKernel.Connectors.SqliteVec`** as the backing store (sqlite-vec under the hood). Implemented in `SqliteMemoryService` (still uses the old name, even though the implementation is now via the abstraction).

**Why:** The user wanted a SQLite-like, file-based local vector DB. `Microsoft.Extensions.VectorData` is Microsoft's first-party abstraction over multiple stores (in-memory, SQLite/sqlite-vec, Qdrant, etc.); committing to it keeps the door open to swapping backends later.

**How to apply:**
- Don't introduce a different vector library (LiteDB, HNSW.Net, Faiss.NET, raw sqlite-vec, DuckDB+VSS, etc.) without re-checking with the user.
- The connector package was renamed from `Microsoft.SemanticKernel.Connectors.Sqlite` → `Microsoft.SemanticKernel.Connectors.SqliteVec`; reach for the latter.
- Don't pin `Microsoft.Extensions.VectorData.Abstractions` explicitly — the SqliteVec connector is built against an older Abstractions (the one that still has `OldFilter`); pinning the latest 10.x breaks at runtime with `MissingMethodException`. Let the connector drag in its expected transitive version.
- `VectorSearchResult.Score` from SqliteVec is **cosine distance** (lower = better, 0 = identical). The service translates to similarity (`1.0 - distance`) at the boundary so callers see the same scale as before (1.0 = identical).
- Embedding dimension is per-agent; the service probes the embedder once per reconcile so the orphan-only-no-files case still works.
