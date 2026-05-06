namespace LlamaShears.Core.Abstractions.Templating;

/// <summary>
/// Renders a template file against an input object. Implementations
/// own the template language (today: Scriban); callers see only the
/// rendered string.
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// Reads the template at <paramref name="templatePath"/>, binds it
    /// to <paramref name="input"/>, and returns the rendered output.
    /// Returns <see langword="null"/> when no file exists at
    /// <paramref name="templatePath"/>; callers handle missing
    /// templates as part of normal control flow rather than via
    /// exceptions.
    /// </summary>
    ValueTask<string?> RenderAsync(string templatePath, object input, CancellationToken cancellationToken);
}
