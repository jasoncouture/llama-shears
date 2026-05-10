using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Reads/writes <see cref="ModelConfiguration"/> with JSON extension-data
/// semantics: every property not explicitly modeled lands in
/// <see cref="ModelConfiguration.Parameters"/> and round-trips on write.
/// </summary>
public sealed class ModelConfigurationJsonConverter : JsonConverter<ModelConfiguration>
{
    /// <inheritdoc/>
    public override ModelConfiguration? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected object start for ModelConfiguration.");
        }

        var nameComparer = options.PropertyNameCaseInsensitive
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        var idName = ConvertName(nameof(ModelConfiguration.Id), options);
        var thinkName = ConvertName(nameof(ModelConfiguration.Think), options);
        var contextLengthName = ConvertName(nameof(ModelConfiguration.ContextLength), options);
        var tokenLimitName = ConvertName(nameof(ModelConfiguration.TokenLimit), options);

        CompositeIdentity? id = null;
        var think = ThinkLevel.None;
        int? contextLength = null;
        var tokenLimit = 0;
        ImmutableDictionary<string, JsonElement>.Builder? parameters = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (id is null)
                {
                    throw new JsonException(string.Concat(
                        "Missing required property '",
                        idName,
                        "' for ModelConfiguration."));
                }
                return new ModelConfiguration(
                    id,
                    think,
                    contextLength,
                    tokenLimit,
                    parameters?.ToImmutable());
            }
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            var propertyName = reader.GetString()!;
            reader.Read();

            if (nameComparer.Equals(propertyName, idName))
            {
                id = JsonSerializer.Deserialize<CompositeIdentity>(ref reader, options);
            }
            else if (nameComparer.Equals(propertyName, thinkName))
            {
                think = JsonSerializer.Deserialize<ThinkLevel>(ref reader, options);
            }
            else if (nameComparer.Equals(propertyName, contextLengthName))
            {
                contextLength = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
            }
            else if (nameComparer.Equals(propertyName, tokenLimitName))
            {
                tokenLimit = reader.TokenType == JsonTokenType.Null ? 0 : reader.GetInt32();
            }
            else
            {
                parameters ??= ImmutableDictionary.CreateBuilder<string, JsonElement>(StringComparer.Ordinal);
                parameters[propertyName] = JsonElement.ParseValue(ref reader);
            }
        }
        throw new JsonException();
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        ModelConfiguration value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(ConvertName(nameof(ModelConfiguration.Id), options));
        JsonSerializer.Serialize(writer, value.Id, options);

        writer.WritePropertyName(ConvertName(nameof(ModelConfiguration.Think), options));
        JsonSerializer.Serialize(writer, value.Think, options);

        writer.WritePropertyName(ConvertName(nameof(ModelConfiguration.ContextLength), options));
        if (value.ContextLength is { } contextLength)
        {
            writer.WriteNumberValue(contextLength);
        }
        else
        {
            writer.WriteNullValue();
        }

        writer.WritePropertyName(ConvertName(nameof(ModelConfiguration.TokenLimit), options));
        writer.WriteNumberValue(value.TokenLimit);

        if (value.Parameters is { Count: > 0 })
        {
            foreach (var (key, element) in value.Parameters)
            {
                writer.WritePropertyName(key);
                element.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
    }

    private static string ConvertName(string property, JsonSerializerOptions options)
        => options.PropertyNamingPolicy?.ConvertName(property) ?? property;
}
