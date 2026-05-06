namespace LlamaShears.Core.Abstractions.SystemPrompt;

/// <summary>
/// Resolves a named system prompt template, renders it against
/// <see cref="SystemPromptTemplateParameters"/>, and returns the body
/// to feed into the model's
/// <see cref="LlamaShears.Core.Abstractions.Provider.ModelRole.System"/>
/// turn. Bodies are stable for the agent's lifetime so the model's
/// prompt-cache prefix stays warm across turns.
/// </summary>
public interface ISystemPromptProvider
{
    /// <summary>
    /// Resolves <paramref name="templateName"/> to its system-prompt
    /// body, rendered against <paramref name="parameters"/>.
    /// <see langword="null"/>, empty, or whitespace names default to
    /// the framework's <c>DEFAULT</c> template. The name must not
    /// contain path separators. Implementations may search multiple
    /// roots and fall back to the framework's bundled default; throw
    /// when no candidate exists.
    /// </summary>
    ValueTask<string> GetAsync(
        string? templateName,
        SystemPromptTemplateParameters parameters,
        CancellationToken cancellationToken);
}
