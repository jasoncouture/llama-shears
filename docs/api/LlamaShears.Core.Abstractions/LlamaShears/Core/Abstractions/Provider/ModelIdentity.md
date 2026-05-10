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

### `ToString`

Renders the identity in wire form
`"<provider>/<model>"`.

#### Returns

The wire-format identity.

### `TryParse`(string id, [ModelIdentity](ModelIdentity.md)& identity)

Attempts to parse a string of the form
`"<provider>/<model>"` into a
[ModelIdentity](ModelIdentity.md).

#### Parameters

- `id` — Wire-format identity to parse.
- `identity` — On success, the parsed identity; on failure, `null`.

#### Returns

`true` when `id` contains the
`provider/model` separator; otherwise `false`.

### `op_Explicit`(ModelIdentity value)

Parses a string of the form `"<provider>/<model>"`
into a [ModelIdentity](ModelIdentity.md). A `null` input
yields a `null` identity; a malformed input throws
FormatException.

#### Parameters

- `value` — Wire-format identity, or `null`.

#### Exceptions

- FormatException — `value` is non-null but does not contain the
`provider/model` separator.

### `op_Implicit`(String identity)

Renders the identity as `"<provider>/<model>"`. A
`null` identity converts to a `null`
string so callers do not need to null-check before formatting.

#### Parameters

- `identity` — Identity to render, or `null`.

