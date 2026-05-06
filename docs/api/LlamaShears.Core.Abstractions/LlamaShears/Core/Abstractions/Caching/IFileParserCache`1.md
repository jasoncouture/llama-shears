# LlamaShears.Core.Abstractions.Caching.IFileParserCache`1

Assembly: `LlamaShears.Core.Abstractions`

Read-through cache for parsing on-disk files. Wraps an
[IShearsCache](IShearsCache`1.md) so the owning `T`
scopes the keyspace exactly as it does for direct cache use.



Each call computes a key from the file's path, existence,
last-write time, and length, prefixed with the requested
`TItem` type. When the key hits, the cached value is returned
without invoking the parser. On miss, the parser receives the open
file stream — or `null` when the file does not
exist — and its result is cached under that key.





Because the key folds in mtime and length, edits to the file
produce a new key on the next call and the previous entry ages out
naturally; callers do not invalidate by hand.

## Type Parameters

- `T` — The owning type. Used as the key prefix.

