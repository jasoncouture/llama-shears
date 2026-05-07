# LlamaShears.Plugins

## Types

- [IPlugin](IPlugin.md) — Contract every plugin exposes. Instances are produced by the assembly's `IPluginLoader<IPlugin>` implementation and threaded through the host's startup, request pipeline, and lifecycle.
- [IServiceCollectionSnapshot](IServiceCollectionSnapshot.md) — Reversible record of an `IServiceCollection`'s descriptor list. Disposing without calling [IServiceCollectionSnapshot](IServiceCollectionSnapshot.md).`AcceptChanges` rolls the collection back to the state captured at snapshot time; calling [IServiceCollectionSnapshot](IServiceCollectionSnapshot.md).`AcceptChanges` commits the current state as the new rollback baseline.

