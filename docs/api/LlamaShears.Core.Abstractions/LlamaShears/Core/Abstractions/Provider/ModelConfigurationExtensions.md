# LlamaShears.Core.Abstractions.Provider.ModelConfigurationExtensions

Assembly: `LlamaShears.Core.Abstractions`

Convenience accessors for pulling the active [ModelConfiguration](ModelConfiguration.md)
off an [IDataContextScope](../Common/IDataContextScope.md) without callers having to remember
the well-known key, plus a flat-schema projection helper for handing the
configuration to a provider-specific options record.

## Methods

### `GetModelConfiguration`([IDataContextScope](../Common/IDataContextScope.md) scope)

Returns the [ModelConfiguration](ModelConfiguration.md) attached to the given scope
under [ModelConfiguration](ModelConfiguration.md).`DataKey`. Throws when the scope is
`null` or has no model configuration stashed; intended
for sites that legitimately cannot proceed without one.

### `TryGetModelConfiguration`([IDataContextScope](../Common/IDataContextScope.md) scope)

Returns the [ModelConfiguration](ModelConfiguration.md) attached to the given scope
under [ModelConfiguration](ModelConfiguration.md).`DataKey`, or `null`
if none is set.

#### Parameters

- `scope` — Data-context scope to inspect.

#### Returns

The active model configuration, or `null` when the scope has none.

