using System.Text.Json.Serialization;

namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// Reasoning effort hint for thinking-capable models. Providers that
/// don't support reasoning levels should ignore the value;
/// <see cref="None"/> means "do not enable reasoning."
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ThinkLevel>))]
public enum ThinkLevel
{
    None = 0,
    Low,
    Medium,
    High,
}
