# LlamaShears.Core.Abstractions.Provider.CompositeIdentityJsonConverter

Assembly: `LlamaShears.Core.Abstractions`

JSON converter for [CompositeIdentity](CompositeIdentity.md): serializes as the
compact string `"provider/model"` rather than as an object.

## Methods

### `Read`(Utf8JsonReader& reader, Type typeToConvert, JsonSerializerOptions options)

### `Write`(Utf8JsonWriter writer, [CompositeIdentity](CompositeIdentity.md) value, JsonSerializerOptions options)

