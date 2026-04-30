using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ModelTurn), "turn")]
public interface IContextEntry
{
    int Version => 1;
}
