# LlamaShears.Core.Abstractions.Caching.IShearsCache<T>

Assembly: `LlamaShears.Core.Abstractions.Caching`

Per-owner view onto the host's shared in-memory cache. The type
parameter `T` identifies the consumer (typically
the calling class, mirroring the `ILogger<T>` pattern); its
full type name is automatically prefixed onto every key so consumers
cannot collide with each other's keyspaces by accident.



Entries are constrained to reference types: most structs do not carry
enough state to warrant caching, and constraining the cached item
type to `class` avoids boxing and the
Nullable wrapping that would otherwise be needed to
express "not present" cleanly.





Time-to-live is absolute: an entry stored with a TTL of `X` is
evicted `X` after [IShearsCache](IShearsCache-1.md).`Set``1` returns, regardless of
reads. Sliding behaviour is up to the caller — re-call
[IShearsCache](IShearsCache-1.md).`Set``1` on hit to refresh.

## Type Parameters

- `T` — The owning type. Used as the key prefix.

## Methods

### `Invalidate`(string cacheKey)

Removes the entry at `cacheKey` for this owner if
one exists. No-op when there is no matching entry.

