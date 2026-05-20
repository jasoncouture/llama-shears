# LlamaShears.Core.CombinedDisposable

Assembly: `LlamaShears.Core.Abstractions`

Extension methods that chain disposables into a single [DisposableList](DisposableList.md).
Folds the resulting list when either side is already a list, so a chain of
`.And(x).And(y).And(z)` stays flat.

## Methods

### `And`([DisposableList](DisposableList.md) current, IAsyncDisposable disposable)

Appends an asynchronous disposable to an existing list.

### `And`([DisposableList](DisposableList.md) current, IDisposable disposable)

Appends a synchronous disposable to an existing list.

### `And`(IAsyncDisposable current, IAsyncDisposable disposable)

Combines two asynchronous disposables.

### `And`(IAsyncDisposable current, IDisposable disposable)

Combines an asynchronous disposable with a synchronous one.

### `And`(IDisposable current, IAsyncDisposable disposable)

Combines a synchronous disposable with an asynchronous one.

### `And`(IDisposable current, IDisposable disposable)

Combines two synchronous disposables.

