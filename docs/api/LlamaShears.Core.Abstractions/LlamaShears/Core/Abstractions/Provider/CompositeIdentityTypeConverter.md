# LlamaShears.Core.Abstractions.Provider.CompositeIdentityTypeConverter

Assembly: `LlamaShears.Core.Abstractions`

TypeConverter for [CompositeIdentity](CompositeIdentity.md) so it
flows through configuration binding and similar string-pivoted
machinery as `"provider/model"`.

## Methods

### `CanConvertFrom`(ITypeDescriptorContext context, Type sourceType)

### `CanConvertTo`(ITypeDescriptorContext context, Type destinationType)

### `ConvertFrom`(ITypeDescriptorContext context, CultureInfo culture, object value)

### `ConvertTo`(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)

