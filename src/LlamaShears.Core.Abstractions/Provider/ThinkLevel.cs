using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

[JsonConverter(typeof(JsonStringEnumConverter<ThinkLevel>))]
public enum ThinkLevel
{
    None = 0,
    Low,
    Medium,
    High,
}
