using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlamaShears.Provider.Abstractions;

public sealed class ModelIdentityJsonConverter : JsonConverter<ModelIdentity>
{
    public override ModelIdentity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                $"Expected a string in the form 'provider/model' for {nameof(ModelIdentity)}; got {reader.TokenType}.");
        }

        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new JsonException($"{nameof(ModelIdentity)} must not be empty.");
        }

        var slash = raw.IndexOf('/');
        if (slash <= 0 || slash >= raw.Length - 1)
        {
            throw new JsonException(
                $"Expected '{raw}' to be in the form 'provider/model' (non-empty on both sides of the first '/').");
        }

        return new ModelIdentity(raw[..slash], raw[(slash + 1)..]);
    }

    public override void Write(Utf8JsonWriter writer, ModelIdentity value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue($"{value.Provider}/{value.Model}");
    }
}
