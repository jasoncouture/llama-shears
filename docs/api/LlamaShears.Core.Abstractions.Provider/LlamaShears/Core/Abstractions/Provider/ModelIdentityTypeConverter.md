# LlamaShears.Core.Abstractions.Provider.ModelIdentityTypeConverter

Assembly: `LlamaShears.Core.Abstractions.Provider`

TypeConverter for [ModelIdentity](ModelIdentity.md) so it
flows through configuration binding and similar string-pivoted
machinery as `"provider/model"`.

## Methods

### `CanConvertFrom`(ITypeDescriptorContext context, Type sourceType)

### `CanConvertTo`(ITypeDescriptorContext context, Type destinationType)

### `ConvertFrom`(ITypeDescriptorContext context, CultureInfo culture, object value)

### `ConvertTo`(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)

