using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

[JsonConverter(typeof(ModelIdentityJsonConverter))]
public sealed record ModelIdentity(string Provider, string Model);
