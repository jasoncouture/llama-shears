# LlamaShears.Core.Abstractions.Common.DataContextServiceCollectionExtensions

Assembly: `LlamaShears.Core.Abstractions`

DI registration helpers for [IDataContextItemProvider](IDataContextItemProvider.md).

## Methods

### `GetScopedDataProviders`(IServiceProvider serviceProvider)

Returns the scoped [IDataContextItemProvider](IDataContextItemProvider.md)
registrations resolvable from `serviceProvider`.
Singleton providers are not returned here — the factory consumes
those directly via its `[FromKeyedServices]` constructor.

