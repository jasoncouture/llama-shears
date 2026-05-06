# LlamaShears.Core.Abstractions.Provider.ModelIdentityJsonConverter

Assembly: `LlamaShears.Core.Abstractions.Provider`

JSON converter for [ModelIdentity](ModelIdentity.md): serializes as the
compact string `"provider/model"` rather than as an object.

## Methods

### `Read`(Utf8JsonReader& reader, Type typeToConvert, JsonSerializerOptions options)

### `Write`(Utf8JsonWriter writer, [ModelIdentity](ModelIdentity.md) value, JsonSerializerOptions options)

