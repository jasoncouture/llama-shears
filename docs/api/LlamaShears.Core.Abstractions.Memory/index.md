# LlamaShears.Core.Abstractions.Memory

Long-term-memory contracts for [LlamaShears](https://github.com/jasoncouture/llama-shears). The implementation under `LlamaShears.Core` keeps an agent's markdown memory files (`<workspace>/memory/YYYY-MM-DD/<unix-seconds>.md`) in sync with a sqlite-vec vector index and serves search queries against that index.

## Public surface

- **`IMemoryStore`** — write a memory; the implementation files it under the conventional path and indexes it.
- **`IMemorySearcher`** — query the index by semantic similarity; results are scored, post-filtered against on-disk presence, and capped at the requested limit.
- **`IMemoryIndexer`** — periodic / on-demand reconciliation between the memory directory and the index.
- **`MemoryRef`** — the lightweight reference returned from a store call (relative path).
- **`MemorySearchResult`** — a ranked hit (relative path + similarity score).
- **`MemoryReconciliation`** — summary of an indexer pass (added / updated / removed / total).

## See also

- [Memory and RAG](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/memory.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.

---

## LlamaShears.Core.Abstractions.Memory

- [IMemoryIndexer](LlamaShears/Core/Abstractions/Memory/IMemoryIndexer.md)
- [IMemorySearcher](LlamaShears/Core/Abstractions/Memory/IMemorySearcher.md)
- [IMemoryStore](LlamaShears/Core/Abstractions/Memory/IMemoryStore.md)
- [MemoryReconciliation](LlamaShears/Core/Abstractions/Memory/MemoryReconciliation.md)
- [MemoryRef](LlamaShears/Core/Abstractions/Memory/MemoryRef.md)
- [MemorySearchResult](LlamaShears/Core/Abstractions/Memory/MemorySearchResult.md)

