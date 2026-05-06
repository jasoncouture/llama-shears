using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Globally unique model identifier formed by pairing a provider name
/// with that provider's model id. Serializes to/from
/// <c>"&lt;provider&gt;/&lt;model&gt;"</c> for human readability and
/// concise JSON.
/// </summary>
/// <param name="Provider">Provider name (matches <see cref="IProviderFactory.Name"/>).</param>
/// <param name="Model">Provider-scoped model identifier.</param>
[JsonConverter(typeof(ModelIdentityJsonConverter))]
[TypeConverter(typeof(ModelIdentityTypeConverter))]
public sealed record ModelIdentity(string Provider, string Model);
