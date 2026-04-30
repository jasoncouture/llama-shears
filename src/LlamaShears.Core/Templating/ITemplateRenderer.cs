namespace LlamaShears.Core.Templating;

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
    /// </summary>
    string Render(string templatePath, object input);
}
