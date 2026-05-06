# LlamaShears.Core.Abstractions.Provider.SupportedInputType

Assembly: `LlamaShears.Core.Abstractions.Provider`

Bit-set describing the modalities a model accepts as input. Used
by the catalog ([ModelInfo](ModelInfo.md).`SupportedInputs`) so
callers can route prompts containing attachments to the right
model without round-tripping the provider.

## Fields

### `Audio`

Audio attachments are supported.

### `Image`

Image attachments are supported.

### `None`

No modalities are supported.

### `Text`

Plain text input is supported.

### `Video`

Video attachments are supported.

