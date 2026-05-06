namespace LlamaShears.Core.Abstractions.PromptContext;

/// <summary>
/// Renders the per-turn ephemeral context block (the harness-injected
/// <c>&lt;system&gt;...&lt;/system&gt;</c> prefix) from the workspace
/// template at <c>system/context/PROMPT.md</c>, with a bundled
/// fallback. Unlike the static system prompt this is volatile — it
/// captures values like the current time and is re-rendered for every
/// inference call.
/// </summary>
public interface IPromptContextProvider
{
    /// <summary>
    /// Renders the prompt context template against
    /// <paramref name="parameters"/>. Returns <see langword="null"/>
    /// when no template is found in either the workspace or the
    /// bundled fallback. An empty rendered body is returned as-is;
    /// callers that want to skip the injection should treat null and
    /// empty alike.
    /// </summary>
    ValueTask<string?> GetAsync(PromptContextParameters parameters, CancellationToken cancellationToken);
}
