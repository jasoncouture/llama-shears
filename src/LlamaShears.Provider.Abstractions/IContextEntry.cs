using System.Text.Json.Serialization;

namespace LlamaShears.Provider.Abstractions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ModelTurn), "turn")]
public interface IContextEntry
{
    int Version => 1;
}
