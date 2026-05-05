using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Abstractions.Templating;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core.SystemPrompt;

public sealed class FilesystemSystemPromptProvider : ISystemPromptProvider
{
    private const string DefaultName = "DEFAULT";
    private const string TemplateExtension = ".md";
    private const string WorkspaceSystemSubpath = "workspace/system";
    private const string DefaultBundledSubpath = "content/templates/workspace/system";

    private readonly IShearsPaths _paths;
    private readonly ITemplateRenderer _renderer;
    private readonly string _bundledRoot;

    public FilesystemSystemPromptProvider(
        IShearsPaths paths,
        ITemplateRenderer renderer,
        IOptions<FilesystemSystemPromptOptions> options)
    {
        _paths = paths;
        _renderer = renderer;
        var configured = options.Value.BundledRoot;
        _bundledRoot = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, DefaultBundledSubpath)
            : configured;
    }

    public async ValueTask<string> GetAsync(
        string? templateName,
        SystemPromptTemplateParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var name = string.IsNullOrWhiteSpace(templateName) ? DefaultName : templateName;
        if (name.AsSpan().IndexOfAny('/', '\\') >= 0)
        {
            throw new ArgumentException(
                $"System prompt template name must not contain path separators (got '{templateName}').",
                nameof(templateName));
        }

        var workspaceRoot = _paths.GetPath(PathKind.Templates, WorkspaceSystemSubpath);
        var fileName = $"{name}{TemplateExtension}";
        var defaultFileName = $"{DefaultName}{TemplateExtension}";

        string[] candidates =
        [
            Path.Combine(workspaceRoot, fileName),
            Path.Combine(workspaceRoot, defaultFileName),
            Path.Combine(_bundledRoot, fileName),
            Path.Combine(_bundledRoot, defaultFileName),
        ];

        foreach (var path in candidates)
        {
            var rendered = await _renderer.RenderAsync(path, parameters, cancellationToken).ConfigureAwait(false);
            if (rendered is not null)
            {
                return rendered;
            }
        }

        throw new FileNotFoundException(
            $"No system prompt template found for '{name}'. Looked under '{workspaceRoot}' and '{_bundledRoot}'.");
    }
}
