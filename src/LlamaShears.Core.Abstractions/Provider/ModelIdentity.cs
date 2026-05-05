using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

[JsonConverter(typeof(ModelIdentityJsonConverter))]
[TypeConverter(typeof(ModelIdentityTypeConverter))]
public sealed record ModelIdentity(string Provider, string Model);
