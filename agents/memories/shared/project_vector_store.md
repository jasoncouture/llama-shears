---
name: Vector store choice — Microsoft.Extensions.VectorData
description: LlamaShears will use Microsoft.Extensions.VectorData for embeddings/vector storage; not yet implemented
type: project
---

LlamaShears will use **Microsoft.Extensions.VectorData** as the vector-store abstraction when vector storage is added. For local/file-based persistence, the intended backing store is the SQLite connector (`Microsoft.SemanticKernel.Connectors.Sqlite`), which wraps **sqlite-vec** under the hood.

**Why:** The user wants a SQLite-like, file-based local vector DB. `Microsoft.Extensions.VectorData` is Microsoft's emerging first-party abstraction over multiple stores (in-memory, SQLite/sqlite-vec, Qdrant, etc.), so picking it now keeps the abstraction stable while leaving room to swap backends later.

**How to apply:** Don't introduce a different vector library (LiteDB, HNSW.Net, Faiss.NET, raw sqlite-vec, DuckDB+VSS, etc.) without re-checking with the user. When work resumes on vector storage, start with `Microsoft.Extensions.VectorData` packages and the SQLite connector. Status: noted but **not yet implemented** — no NuGet packages added or code written for this as of 2026-04-27.
