# LlamaShears.Core.Abstractions.Caching

Generic caching contracts for [LlamaShears](https://github.com/jasoncouture/llama-shears). Small, dependency-free abstractions used by the host's own caches and available to plugins that want to cache their own work behind the same shape.

## Public surface

- **`IShearsCache<T>`** — type-scoped cache; namespaced under `T`'s full name so two unrelated cache users can't collide.
- **`IFileParserCache`** — file-keyed parse-result cache; consumers pass a `TState` (record struct preferred) and a static parser delegate so the cache can avoid closure allocations.
- **`CacheResult<TItem>`** — three-state result: present-with-value, present-but-type-mismatch, absent. Distinguishes "cache miss" from "cached the wrong shape."

## See also

- [Architecture overview](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/architecture.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.

---

## LlamaShears.Core.Abstractions.Caching

- [CacheResult<T>](LlamaShears/Core/Abstractions/Caching/CacheResult-1.md)
- [IFileParserCache<T>](LlamaShears/Core/Abstractions/Caching/IFileParserCache-1.md)
- [IShearsCache<T>](LlamaShears/Core/Abstractions/Caching/IShearsCache-1.md)

