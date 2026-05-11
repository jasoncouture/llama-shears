# LlamaShears.Core.Abstractions.Common.DataContextScopeFactoryExtensions

Assembly: `LlamaShears.Core.Abstractions`

Composition helpers that pair a DI IServiceScope with
a new [IDataContextScope](IDataContextScope.md).`BeginScope` frame so the two
share a single disposable lifetime.

## Methods

### `ApplyScopeDataAsync`(IServiceScope scope, CancellationToken cancellationToken)

Resolves every scoped [IDataContextItemProvider](IDataContextItemProvider.md)
registered against `scope`'s provider and
applies each to the [IDataContextScope](IDataContextScope.md) resolved
from the same provider.

### `CreateAsyncScopeWithData`(IServiceScopeFactory factory)

Creates an AsyncServiceScope and opens a new
[IDataContextScope](IDataContextScope.md).`BeginScope` frame on the data scope
resolved from the new DI scope. The returned bundle disposes both
(data frame first, then the DI scope) when disposed.

