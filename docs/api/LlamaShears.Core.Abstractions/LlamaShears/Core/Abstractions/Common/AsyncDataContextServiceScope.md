# LlamaShears.Core.Abstractions.Common.AsyncDataContextServiceScope

Assembly: `LlamaShears.Core.Abstractions`

Decorator that owns an AsyncServiceScope alongside an
IDisposable data-context frame. Disposing the decorator
tears down the data frame and the DI scope as one operation.

## Properties

### `ServiceProvider`

The DI service scope's provider.

### `ServiceScope`

The underlying DI service scope.

## Methods

### `DisposeAsync`

