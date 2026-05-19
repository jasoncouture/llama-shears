using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Construction-time inputs for <see cref="IProviderFactory.CreateModel"/>
/// and <see cref="IEmbeddingProviderFactory.CreateModel"/>. Doubles as the
/// authored agent-config shape — the same record persists to disk and flows
/// to providers verbatim.
/// </summary>
/// <param name="Id">Globally unique model identifier (provider + provider-scoped model name).</param>
/// <param name="Think">Thinking effort hint (chat models only).</param>
/// <param name="ContextLength">Override for the model's context-window size; <see langword="null"/> uses provider default.</param>
/// <param name="TokenLimit">Maximum response tokens; <c>0</c> = unbounded.</param>
/// <param name="Parameters">Free-form provider-specific overrides. Captures every JSON property that does not match a known field; providers consume entries (e.g. Ollama reads <c>keepAlive</c>).</param>
[JsonConverter(typeof(ModelConfigurationJsonConverter))]
public record ModelConfiguration(
    [property: JsonRequired] CompositeIdentity Id,
    ThinkLevel Think = ThinkLevel.None,
    int? ContextLength = null,
    int TokenLimit = 0,
    ImmutableDictionary<string, JsonElement>? Parameters = null
)
{
    /// <summary>Key used to stash the active <see cref="ModelConfiguration"/> in the per-turn data context scope.</summary>
    public const string DataKey = "model_configuration";
}
