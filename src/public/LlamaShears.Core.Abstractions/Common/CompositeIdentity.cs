using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Common;

/// <summary>
/// Two-part identifier that pairs a provider name with a provider-scoped
/// model id to form a globally unique handle. Serializes to/from
/// <c>"&lt;provider&gt;/&lt;model&gt;"</c> for human readability and
/// concise JSON.
/// </summary>
/// <param name="Provider">Provider name.</param>
/// <param name="Model">Provider-scoped model identifier.</param>
[JsonConverter(typeof(CompositeIdentityJsonConverter))]
[TypeConverter(typeof(CompositeIdentityTypeConverter))]
public sealed record CompositeIdentity(string Provider, string Model)
{
    /// <summary>
    /// Renders the identity in wire form
    /// <c>"&lt;provider&gt;/&lt;model&gt;"</c>.
    /// </summary>
    /// <returns>The wire-format identity.</returns>
    public override string ToString() => $"{Provider}/{Model}";

    /// <summary>
    /// Renders the identity as <c>"&lt;provider&gt;/&lt;model&gt;"</c>. A
    /// <see langword="null"/> identity converts to a <see langword="null"/>
    /// string so callers do not need to null-check before formatting.
    /// </summary>
    /// <param name="identity">Identity to render, or <see langword="null"/>.</param>
    public static implicit operator string?(CompositeIdentity? identity) => identity?.ToString();

    /// <summary>
    /// Parses a string of the form <c>"&lt;provider&gt;/&lt;model&gt;"</c>
    /// into a <see cref="CompositeIdentity"/>. A <see langword="null"/> input
    /// yields a <see langword="null"/> identity; a malformed input throws
    /// <see cref="FormatException"/>.
    /// </summary>
    /// <param name="value">Wire-format identity, or <see langword="null"/>.</param>
    /// <exception cref="FormatException">
    /// <paramref name="value"/> is non-null but does not contain the
    /// <c>provider/model</c> separator.
    /// </exception>
    public static explicit operator CompositeIdentity?(string? value)
    {
        if (value is null)
        {
            return null;
        }
        if (!TryParse(value, out var identity))
        {
            throw new FormatException(
                $"'{value}' is not a valid model identity; expected '<provider>/<model>'.");
        }
        return identity;
    }

    /// <summary>
    /// Attempts to parse a string of the form
    /// <c>"&lt;provider&gt;/&lt;model&gt;"</c> into a
    /// <see cref="CompositeIdentity"/>.
    /// </summary>
    /// <param name="id">Wire-format identity to parse.</param>
    /// <param name="identity">
    /// On success, the parsed identity; on failure, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="id"/> contains the
    /// <c>provider/model</c> separator; otherwise <see langword="false"/>.
    /// </returns>
    public static bool TryParse(string id, [NotNullWhen(true)] out CompositeIdentity? identity)
    {
        identity = null;
        var parts = id.Split('/', 2);
        if (parts.Length != 2)
        {
            return false;
        }
        identity = new CompositeIdentity(parts[0], parts[1]);
        return true;
    }
}
