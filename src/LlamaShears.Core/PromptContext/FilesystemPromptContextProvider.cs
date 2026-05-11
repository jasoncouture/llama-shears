using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.SystemPrompt;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core.PromptContext;

public sealed class FilesystemPromptContextProvider : IPromptContextProvider
{
    private const string DefaultName = "PROMPT";
    private const string TemplateExtension = ".md";
    private const string WorkspaceContextSubpath = "workspace/system/context";
    private const string DefaultBundledSubpath = "content/templates/workspace/system/context";

    private readonly IShearsPaths _paths;
    private readonly ITemplateRenderer _renderer;
    private readonly string _bundledRoot;

    public FilesystemPromptContextProvider(
        IShearsPaths paths,
        ITemplateRenderer renderer,
        IOptions<FilesystemPromptContextOptions> options)
    {
        _paths = paths;
        _renderer = renderer;
        var configured = options.Value.BundledRoot;
        _bundledRoot = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, DefaultBundledSubpath)
            : configured;
    }

    public async ValueTask<string?> GetAsync(
        string? templateName,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);

        var name = string.IsNullOrWhiteSpace(templateName) ? DefaultName : templateName;
        if (name.AsSpan().IndexOfAny('/', '\\') >= 0)
        {
            throw new ArgumentException(
                $"Prompt-context template name must not contain path separators (got '{templateName}').",
                nameof(templateName));
        }

        var workspaceRoot = _paths.GetPath(PathKind.Templates, WorkspaceContextSubpath);
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
            var rendered = await _renderer.RenderAsync(path, data, cancellationToken);
            if (rendered is not null)
            {
                return rendered;
            }
        }
        return null;
    }
}
