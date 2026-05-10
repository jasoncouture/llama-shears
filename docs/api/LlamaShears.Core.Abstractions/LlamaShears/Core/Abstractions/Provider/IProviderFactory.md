# LlamaShears.Core.Abstractions.Provider.IProviderFactory

Assembly: `LlamaShears.Core.Abstractions`

Plugin contract for a language-model provider. Surfaces the catalog
of models the provider can serve and constructs
[ILanguageModel](ILanguageModel.md) instances from
[ModelConfiguration](ModelConfiguration.md). One factory per provider (Ollama,
future cloud providers, etc.).

## Properties

### `Name`

Unique name of the provider factory. Must match
`^[A-Z]([A-Z0-9-_]*)[A-Z0-9]+$`; factories with a non-matching
name are ignored.

## Methods

### `CreateModel`([ModelConfiguration](ModelConfiguration.md) configuration)

Creates a model instance from `configuration`.

### `ListModelsAsync`(CancellationToken cancellationToken)

Lists every model the provider surfaces, with metadata.

