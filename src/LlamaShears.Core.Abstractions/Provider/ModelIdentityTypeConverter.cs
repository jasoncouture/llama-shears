using System.ComponentModel;
using System.Globalization;

namespace LlamaShears.Core.Abstractions.Provider;

public sealed class ModelIdentityTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string raw)
        {
            return base.ConvertFrom(context, culture, value);
        }
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new FormatException($"{nameof(ModelIdentity)} must not be empty.");
        }
        var slash = raw.IndexOf('/');
        if (slash <= 0 || slash >= raw.Length - 1)
        {
            throw new FormatException(
                $"Expected '{raw}' to be in the form 'provider/model' (non-empty on both sides of the first '/').");
        }
        return new ModelIdentity(raw[..slash], raw[(slash + 1)..]);
    }

    public override object? ConvertTo(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value,
        Type destinationType)
    {
        if (destinationType == typeof(string) && value is ModelIdentity id)
        {
            return $"{id.Provider}/{id.Model}";
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
