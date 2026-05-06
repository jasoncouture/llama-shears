# LlamaShears.Core.Abstractions.Provider.IEmbeddingProviderFactory

Assembly: `LlamaShears.Core.Abstractions`

Plugin contract for an embedding provider, parallel to
[IProviderFactory](IProviderFactory.md). A given provider may implement
chat ([IProviderFactory](IProviderFactory.md)), embeddings (this interface),
or both, registering each implemented contract separately into DI.

## Properties

### `Name`

Unique name of the provider. Same constraint as
[IProviderFactory](IProviderFactory.md).`Name`; the embedding factory and
chat factory for the same provider share the name.

## Methods

### `CreateModel`([ModelConfiguration](ModelConfiguration.md) configuration)

Creates an embedding model from `configuration`.

### `ListModelsAsync`(CancellationToken cancellationToken)

Lists every embedding-capable model the provider surfaces, with
metadata.

