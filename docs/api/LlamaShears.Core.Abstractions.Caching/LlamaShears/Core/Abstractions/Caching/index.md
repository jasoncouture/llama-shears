# LlamaShears.Core.Abstractions.Caching

## Types

- [CacheResult<T>](CacheResult-1.md) — Outcome of [IShearsCache](IShearsCache-1.md).`TryGet``1`: distinguishes "no entry", "entry exists under a different type", and "entry hit" so callers can branch without exception handling.
- [IFileParserCache<T>](IFileParserCache-1.md) — Read-through cache for parsing on-disk files. Wraps an [IShearsCache](IShearsCache-1.md) so the owning `T` scopes the keyspace exactly as it does for direct cache use.
- [IShearsCache<T>](IShearsCache-1.md) — Per-owner view onto the host's shared in-memory cache. The type parameter `T` identifies the consumer (typically the calling class, mirroring the `ILogger<T>` pattern); its full type name is automatically prefixed onto every key so consumers cannot collide with each other's keyspaces by accident.

