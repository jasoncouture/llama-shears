using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Base contract for any entry that can be appended to an agent's
/// conversation log. Polymorphic JSON serialization is keyed by the
/// <c>kind</c> discriminator on the wire.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ModelTurn), "turn")]
[JsonDerivedType(typeof(ModelTokenInformationContextEntry), "token_metrics")]
public interface IContextEntry
{
    /// <summary>
    /// Schema version for the concrete entry shape. Implementations bump
    /// this when the entry's serialized form changes incompatibly so
    /// readers can detect and migrate older payloads.
    /// </summary>
    int Version => 1;
}