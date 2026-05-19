# LlamaShears.Core.DisposableList

Assembly: `LlamaShears.Core.Abstractions`

Composite disposable that owns a LIFO stack of mixed IDisposable and
IAsyncDisposable instances. Disposal walks the stack in reverse order,
catches per-item exceptions, and rethrows as an AggregateException.
Sync IDisposable.`Dispose` blocks on the async path — async-first by design.

## Methods

### `Create`

Allocates a new, empty [DisposableList](DisposableList.md).

### `DisposeAsync`

Disposes every entry in reverse insertion order, aggregating any thrown exceptions.

