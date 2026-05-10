# LlamaShears.Core.Abstractions.Provider.ModelInfo

Assembly: `LlamaShears.Core.Abstractions`

Catalog entry returned by a provider's
[IProviderFactory](IProviderFactory.md).`ListModelsAsync`. Enough metadata for
callers to choose a model without actually loading it.

## Parameters

- `ModelId` — Provider-scoped identifier; pair with the provider name to get a globally unique [CompositeIdentity](CompositeIdentity.md).
- `DisplayName` — Human-readable name surfaced in UI.
- `Description` — Optional longer-form description; `null` when the provider does not supply one.
- `SupportedInputs` — Bit-set of input modalities this model accepts.
- `SupportsReasoning` — Whether the model exposes a thinking / chain-of-thought channel.
- `MaxContextWindow` — Maximum prompt-token budget for a single call.

## Properties

### `Description`

Optional longer-form description; `null` when the provider does not supply one.

### `DisplayName`

Human-readable name surfaced in UI.

### `MaxContextWindow`

Maximum prompt-token budget for a single call.

### `ModelId`

Provider-scoped identifier; pair with the provider name to get a globally unique [CompositeIdentity](CompositeIdentity.md).

### `SupportedInputs`

Bit-set of input modalities this model accepts.

### `SupportsReasoning`

Whether the model exposes a thinking / chain-of-thought channel.

## Methods

### `ModelInfo`(string ModelId, string DisplayName, string Description, [SupportedInputType](SupportedInputType.md) SupportedInputs, bool SupportsReasoning, int MaxContextWindow)

Catalog entry returned by a provider's
[IProviderFactory](IProviderFactory.md).`ListModelsAsync`. Enough metadata for
callers to choose a model without actually loading it.

#### Parameters

- `ModelId` — Provider-scoped identifier; pair with the provider name to get a globally unique [CompositeIdentity](CompositeIdentity.md).
- `DisplayName` — Human-readable name surfaced in UI.
- `Description` — Optional longer-form description; `null` when the provider does not supply one.
- `SupportedInputs` — Bit-set of input modalities this model accepts.
- `SupportsReasoning` — Whether the model exposes a thinking / chain-of-thought channel.
- `MaxContextWindow` — Maximum prompt-token budget for a single call.

