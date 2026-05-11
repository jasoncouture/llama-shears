using LlamaShears.Core.Abstractions.SystemPrompt;

namespace LlamaShears.Core.SystemPrompt;

public sealed class FilesystemSystemPromptProvider : ISystemPromptProvider
{
    private const string DefaultFileName = "DEFAULT.md";

    private readonly ITemplateRenderer _renderer;
    private readonly ITemplateFileLocator _locator;

    public FilesystemSystemPromptProvider(
        ITemplateRenderer renderer,
        ITemplateFileLocator locator)
    {
        _renderer = renderer;
        _locator = locator;
    }

    public async ValueTask<string> GetAsync(
        string? templateFileName,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);

        var fileName = string.IsNullOrWhiteSpace(templateFileName) ? DefaultFileName : templateFileName;
        if (fileName.AsSpan().IndexOfAny('/', '\\') >= 0)
        {
            throw new ArgumentException(
                $"System prompt template name must not contain path separators (got '{templateFileName}').",
                nameof(templateFileName));
        }

        var resolved = _locator.Locate(subFolder: null, fileName, DefaultFileName)
            ?? throw new FileNotFoundException(
                $"No system prompt template found for '{fileName}' across workspace, templates, or bundled roots.");

        var rendered = await _renderer.RenderAsync(resolved, data, cancellationToken).ConfigureAwait(false)
            ?? throw new FileNotFoundException(
                $"System prompt template '{resolved}' could not be rendered.");
        return rendered;
    }
}
