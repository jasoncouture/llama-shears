namespace LlamaShears.Core.Abstractions.SystemPrompt;

/// <summary>
/// Renders a template file against a string-keyed data bag. Implementations
/// own the template language (today: Scriban); callers see only the
/// rendered string. The bag is the full template input — the renderer
/// does not resolve values itself, callers materialize whatever the
/// template needs and hand it in.
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// Renders the template at <paramref name="templatePath"/> against
    /// <paramref name="data"/>.
    /// </summary>
    /// <param name="templatePath">Path of the template file to render.</param>
    /// <param name="data">Template parameters made available under their keys inside the template scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rendered template, or <see langword="null"/> when the template resolves to nothing.</returns>
    ValueTask<string?> RenderAsync(
        string templatePath,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken);
}
