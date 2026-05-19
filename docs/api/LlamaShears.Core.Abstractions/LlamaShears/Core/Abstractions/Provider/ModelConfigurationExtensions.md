# LlamaShears.Core.Abstractions.Provider.ModelConfigurationExtensions

Assembly: `LlamaShears.Core.Abstractions`

Convenience accessors for pulling the active [ModelConfiguration](ModelConfiguration.md)
off an [IDataContextScope](../Common/IDataContextScope.md) without callers having to remember
the well-known key, plus a flat-schema projection helper for handing the
configuration to a provider-specific options record.

## Methods

### `GetModelConfiguration`([IDataContextScope](../Common/IDataContextScope.md) scope)

### `TryGetModelConfiguration`([IDataContextScope](../Common/IDataContextScope.md) scope)

