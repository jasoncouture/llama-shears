using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Hint to a thinking-capable provider for how aggressively the model
/// should reason before answering. Providers that do not support
/// configurable thinking ignore non-<see cref="None"/> values.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ThinkLevel>))]
public enum ThinkLevel
{
    /// <summary>Thinking is disabled (or unsupported by the model).</summary>
    None = 0,
    /// <summary>Cheapest non-zero thinking effort.</summary>
    Low,
    /// <summary>Default thinking effort.</summary>
    Medium,
    /// <summary>Most expensive thinking effort.</summary>
    High,
}
