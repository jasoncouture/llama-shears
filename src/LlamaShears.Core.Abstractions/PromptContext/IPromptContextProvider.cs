namespace LlamaShears.Core.Abstractions.PromptContext;

/// <summary>
/// Renders the per-turn ephemeral context block (the harness-injected
/// <c>&lt;system&gt;...&lt;/system&gt;</c> prefix) from a workspace
/// template under <c>system/context/</c>, with a bundled fallback.
/// Unlike the static system prompt this is volatile — it captures
/// values like the current time and is re-rendered for every
/// inference call.
/// </summary>
public interface IPromptContextProvider
{
    /// <summary>
    /// Renders the prompt-context template named by
    /// <paramref name="templateName"/> (e.g. <c>"PROMPT"</c>) against
    /// <paramref name="parameters"/>. The provider looks for the
    /// template under the workspace's <c>system/context/</c> directory
    /// first, then the bundled fallback, falling back to the default
    /// (<c>PROMPT.md</c>) name at each layer if the requested name is
    /// missing. Returns <see langword="null"/> when nothing is found
    /// in any candidate location. An empty rendered body is returned
    /// as-is; callers that want to skip the injection should treat
    /// null and empty alike.
    /// </summary>
    ValueTask<string?> GetAsync(string? templateName, PromptContextParameters parameters, CancellationToken cancellationToken);
}
