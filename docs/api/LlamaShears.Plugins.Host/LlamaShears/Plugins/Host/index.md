# LlamaShears.Plugins.Host

## Types

- [DeferredLogEntry](DeferredLogEntry.md) — One captured log call. [DeferredLogEntry](DeferredLogEntry.md).`Format` follows the .NET `ILogger` message-template convention; [DeferredLogEntry](DeferredLogEntry.md).`Data` carries the corresponding positional arguments.
- [DeferredPluginHostLogger](DeferredPluginHostLogger.md) — Buffers plugin-host log calls into an in-memory queue so they can be handed off to a real logger once one exists (typically after DI is built). Entries are recorded in arrival order; concurrent callers serialize on a monitor lock.
- [IServiceCollectionSnapshot](IServiceCollectionSnapshot.md) — Reversible record of an `IServiceCollection`'s descriptor list. Disposing without calling [IServiceCollectionSnapshot](IServiceCollectionSnapshot.md).`AcceptChanges` rolls the collection back to the state captured at snapshot time; calling [IServiceCollectionSnapshot](IServiceCollectionSnapshot.md).`AcceptChanges` commits the current state as the new rollback baseline.

