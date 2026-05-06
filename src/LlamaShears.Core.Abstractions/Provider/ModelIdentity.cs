using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Composite identifier for a model: which provider hosts it, and the
/// provider-local model id. Serializes as <c>"provider/model"</c>; the
/// first slash separates the provider from the rest. Provider names
/// must match <see cref="IProviderFactory.Name"/>'s regex (no slashes),
/// which makes the split unambiguous.
/// </summary>
[JsonConverter(typeof(ModelIdentityJsonConverter))]
public sealed record ModelIdentity(string Provider, string Model);
