# LlamaShears.Core.Abstractions.Common.CompositeIdentity

Assembly: `LlamaShears.Core.Abstractions`

Two-part identifier that pairs a provider name with a provider-scoped
model id to form a globally unique handle. Serializes to/from
`"<provider>/<model>"` for human readability and
concise JSON.

## Parameters

- `Provider` — Provider name.
- `Model` — Provider-scoped model identifier.

## Properties

### `Model`

Provider-scoped model identifier.

### `Provider`

Provider name.

## Methods

### `CompositeIdentity`(string Provider, string Model)

Two-part identifier that pairs a provider name with a provider-scoped
model id to form a globally unique handle. Serializes to/from
`"<provider>/<model>"` for human readability and
concise JSON.

#### Parameters

- `Provider` — Provider name.
- `Model` — Provider-scoped model identifier.

### `ToString`

Renders the identity in wire form
`"<provider>/<model>"`.

#### Returns

The wire-format identity.

### `TryParse`(string id, [CompositeIdentity](CompositeIdentity.md)& identity)

Attempts to parse a string of the form
`"<provider>/<model>"` into a
[CompositeIdentity](CompositeIdentity.md).

#### Parameters

- `id` — Wire-format identity to parse.
- `identity` — On success, the parsed identity; on failure, `null`.

#### Returns

`true` when `id` contains the
`provider/model` separator; otherwise `false`.

### `op_Explicit`(CompositeIdentity value)

Parses a string of the form `"<provider>/<model>"`
into a [CompositeIdentity](CompositeIdentity.md). A `null` input
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

