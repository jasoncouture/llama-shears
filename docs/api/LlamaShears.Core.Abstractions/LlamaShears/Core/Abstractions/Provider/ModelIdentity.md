# LlamaShears.Core.Abstractions.Provider.ModelIdentity

Assembly: `LlamaShears.Core.Abstractions`

Globally unique model identifier formed by pairing a provider name
with that provider's model id. Serializes to/from
`"<provider>/<model>"` for human readability and
concise JSON.

## Parameters

- `Provider` — Provider name (matches [IProviderFactory](IProviderFactory.md).`Name`).
- `Model` — Provider-scoped model identifier.

## Properties

### `Model`

Provider-scoped model identifier.

### `Provider`

Provider name (matches [IProviderFactory](IProviderFactory.md).`Name`).

## Methods

### `ModelIdentity`(string Provider, string Model)

Globally unique model identifier formed by pairing a provider name
with that provider's model id. Serializes to/from
`"<provider>/<model>"` for human readability and
concise JSON.

#### Parameters

- `Provider` — Provider name (matches [IProviderFactory](IProviderFactory.md).`Name`).
- `Model` — Provider-scoped model identifier.

