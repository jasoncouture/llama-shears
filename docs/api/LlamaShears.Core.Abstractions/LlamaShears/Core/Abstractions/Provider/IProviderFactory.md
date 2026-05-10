# LlamaShears.Core.Abstractions.Provider.IProviderFactory

Assembly: `LlamaShears.Core.Abstractions`

Plugin contract for a language-model provider. Surfaces the catalog
of models the provider can serve and constructs
[ILanguageModel](ILanguageModel.md) instances from
[ModelConfiguration](ModelConfiguration.md). One factory per provider (Ollama,
future cloud providers, etc.).

## Properties

### `Name`

Unique name of the provider factory. Compared case-insensitively
against [CompositeIdentity](../Common/CompositeIdentity.md).`Provider` when routing.

## Methods

### `CreateModel`([ModelConfiguration](ModelConfiguration.md) configuration)

Creates a model instance from `configuration`.

### `ListModelsAsync`(CancellationToken cancellationToken)

Lists every model the provider surfaces, with metadata.

### `ValidateAsync`([ModelConfiguration](ModelConfiguration.md) configuration, CancellationToken cancellationToken)

Asks the provider to validate `configuration`. Today
the only check is that the model identified by
[ModelConfiguration](ModelConfiguration.md).`ModelId` exists in the provider's
catalogue; the contract is shaped so future implementations can
surface additional reasons (token-limit ceilings, parameter
compatibility, etc.) without an interface change.

#### Parameters

- `configuration` — Configuration to validate.
- `cancellationToken` — Cancellation token.

#### Returns

ValidationResult.`Success` (i.e. `null`) when
the configuration is valid; otherwise a populated
ValidationResult whose ValidationResult.`ErrorMessage`
explains the failure.

