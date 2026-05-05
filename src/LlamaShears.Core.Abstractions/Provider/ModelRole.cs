using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

[JsonConverter(typeof(JsonStringEnumConverter<ModelRole>))]
public enum ModelRole
{
    System,
    User,
    Assistant,
    FrameworkUser,
    FrameworkAssistant,
    Thought,
    Tool,
}
