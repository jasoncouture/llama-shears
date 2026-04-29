namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// Reasoning effort hint for thinking-capable models. Providers that
/// don't support reasoning levels should ignore the value;
/// <see cref="None"/> means "do not enable reasoning."
/// </summary>
public enum ThinkLevel
{
    None = 0,
    Low,
    Medium,
    High,
}
