using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// JSON converter for <see cref="CompositeIdentity"/>: serializes as the
/// compact string <c>"provider/model"</c> rather than as an object.
/// </summary>
public sealed class CompositeIdentityJsonConverter : JsonConverter<CompositeIdentity>
{
    /// <inheritdoc/>
    public override CompositeIdentity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                $"Expected a string in the form 'provider/model' for {nameof(CompositeIdentity)}; got {reader.TokenType}.");
        }

        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new JsonException($"{nameof(CompositeIdentity)} must not be empty.");
        }

        var slash = raw.IndexOf('/');
        if (slash <= 0 || slash >= raw.Length - 1)
        {
            throw new JsonException(
                $"Expected '{raw}' to be in the form 'provider/model' (non-empty on both sides of the first '/').");
        }

        return new CompositeIdentity(raw[..slash], raw[(slash + 1)..]);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, CompositeIdentity value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue($"{value.Provider}/{value.Model}");
    }
}
