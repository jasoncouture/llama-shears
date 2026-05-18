# LlamaShears.Core.Abstractions.Provider.EmbeddingModelConfigurationExtensions

Assembly: `LlamaShears.Core.Abstractions`

Convenience accessors for the asymmetric-embedding prefix knobs that
agents stash inside [ModelConfiguration](ModelConfiguration.md).`Parameters`. The
values are provider-specific, so they ride the free-form parameter
dictionary rather than earning dedicated record fields.

## Fields

### `DocumentPrefixKey`

Parameter key for the asymmetric document prefix.

### `QueryPrefixKey`

Parameter key for the asymmetric query prefix.

## Methods

### `GetDocumentPrefix`([ModelConfiguration](ModelConfiguration.md) configuration)

### `GetQueryPrefix`([ModelConfiguration](ModelConfiguration.md) configuration)

