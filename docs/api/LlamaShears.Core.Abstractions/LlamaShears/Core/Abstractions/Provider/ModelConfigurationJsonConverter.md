# LlamaShears.Core.Abstractions.Provider.ModelConfigurationJsonConverter

Assembly: `LlamaShears.Core.Abstractions`

Reads/writes [ModelConfiguration](ModelConfiguration.md) with JSON extension-data
semantics: every property not explicitly modeled lands in
[ModelConfiguration](ModelConfiguration.md).`Parameters` and round-trips on write.

## Methods

### `Read`(Utf8JsonReader& reader, Type typeToConvert, JsonSerializerOptions options)

### `Write`(Utf8JsonWriter writer, [ModelConfiguration](ModelConfiguration.md) value, JsonSerializerOptions options)

