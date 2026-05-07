# LlamaShears.Plugins.Host.IServiceCollectionSnapshot

Assembly: `LlamaShears.Plugins.Host`

Reversible record of an `IServiceCollection`'s descriptor list.
Disposing without calling [IServiceCollectionSnapshot](IServiceCollectionSnapshot.md).`AcceptChanges` rolls the
collection back to the state captured at snapshot time; calling
[IServiceCollectionSnapshot](IServiceCollectionSnapshot.md).`AcceptChanges` commits the current state as the new
rollback baseline.

## Methods

### `AcceptChanges`

Replaces the snapshot baseline with the underlying collection's
current state. After this call, disposal will not roll back any
changes made up to this point.

### `Rollback`

Restores the underlying collection to the captured baseline.
Equivalent to IDisposable.`Dispose`.

