# LlamaShears.Core.Abstractions.Provider.IEmbeddingProviderFactory

Assembly: `LlamaShears.Core.Abstractions`

Plugin contract for an embedding provider, parallel to
[IProviderFactory](IProviderFactory.md). A given provider may implement
chat ([IProviderFactory](IProviderFactory.md)), embeddings (this interface),
or both, registering each implemented contract separately into DI.

## Properties

### `Name`

Unique name of the provider; the embedding factory and chat factory
for the same provider share the name. Compared case-insensitively
against [CompositeIdentity](CompositeIdentity.md).`Provider` when routing.

## Methods

### `CreateModel`([ModelConfiguration](ModelConfiguration.md) configuration)

Creates an embedding model from `configuration`.

### `ListModelsAsync`(CancellationToken cancellationToken)

Lists every embedding-capable model the provider surfaces, with
metadata.

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

