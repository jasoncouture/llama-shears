using System.ComponentModel;
using System.Globalization;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// <see cref="TypeConverter"/> for <see cref="CompositeIdentity"/> so it
/// flows through configuration binding and similar string-pivoted
/// machinery as <c>"provider/model"</c>.
/// </summary>
public sealed class CompositeIdentityTypeConverter : TypeConverter
{
    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string raw)
        {
            return base.ConvertFrom(context, culture, value);
        }
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new FormatException($"{nameof(CompositeIdentity)} must not be empty.");
        }
        var slash = raw.IndexOf('/');
        if (slash <= 0 || slash >= raw.Length - 1)
        {
            throw new FormatException(
                $"Expected '{raw}' to be in the form 'provider/model' (non-empty on both sides of the first '/').");
        }
        return new CompositeIdentity(raw[..slash], raw[(slash + 1)..]);
    }

    /// <inheritdoc/>
    public override object? ConvertTo(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value,
        Type destinationType)
    {
        if (destinationType == typeof(string) && value is CompositeIdentity id)
        {
            return $"{id.Provider}/{id.Model}";
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
