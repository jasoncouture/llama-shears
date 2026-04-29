using System.Text.Json.Serialization;

namespace LlamaShears.Provider.Abstractions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ModelTurn), "turn")]
public interface IConversationEntry
{
    int Version => 1;
}
