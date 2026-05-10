using System.Text.Json;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Construction-time inputs for <see cref="IProviderFactory.CreateModel"/>
/// and <see cref="IEmbeddingProviderFactory.CreateModel"/>.
/// </summary>
/// <param name="ModelId">Globally unique model identifier (provider + provider-scoped model name).</param>
/// <param name="Think">Thinking effort hint (chat models only).</param>
/// <param name="ContextLength">Override for the model's context-window size; <see langword="null"/> uses provider default.</param>
/// <param name="KeepAlive">Provider-specific keep-alive; <see langword="null"/> uses provider default.</param>
/// <param name="Parameters">Free-form factory-level parameters.</param>
/// <param name="TokenLimit">Maximum response tokens; <c>0</c> = unbounded.</param>
/// <param name="AgentOptions">Agent-supplied JSON options merged on top of the provider's host defaults at request time.</param>
public record ModelConfiguration(
    CompositeIdentity ModelId,
    ThinkLevel Think = ThinkLevel.None,
    int? ContextLength = null,
    TimeSpan? KeepAlive = null,
    IReadOnlyDictionary<string, object>? Parameters = null,
    int TokenLimit = 0,
    JsonElement? AgentOptions = null
)
{
    /// <summary>Key used to stash the active <see cref="ModelConfiguration"/> in the per-turn data context scope.</summary>
    public const string DataKey = "model_configuration";
}
