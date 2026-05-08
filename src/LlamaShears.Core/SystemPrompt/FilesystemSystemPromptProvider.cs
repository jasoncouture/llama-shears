using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.SystemPrompt;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core.SystemPrompt;

public sealed class FilesystemSystemPromptProvider : ISystemPromptProvider
{
    private const string DefaultName = "DEFAULT";
    private const string TemplateExtension = ".md";
    private const string WorkspaceSystemSubpath = "workspace/system";
    private const string DefaultBundledSubpath = "content/templates/workspace/system";

    // Conventional workspace files surfaced into the system prompt as
    // `files`. Order matters: the template renders Files in this order,
    // so BOOTSTRAP comes first when present (a one-shot the agent acts
    // on), then identity, then soul.
    private static readonly ImmutableArray<string> _workspaceFileNames =
    [
        "AGENTS.md",
        "BOOTSTRAP.md",
        "IDENTITY.md",
        "SOUL.md",
        "USER.md"
    ];

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

        var enriched = parameters with
        {
            Files = await ReadWorkspaceFilesAsync(parameters.WorkspacePath, cancellationToken)
                .ConfigureAwait(false),
        };

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
            var rendered = await _renderer.RenderAsync(path, enriched, cancellationToken).ConfigureAwait(false);
            if (rendered is not null)
            {
                return rendered;
            }
        }

        throw new FileNotFoundException(
            $"No system prompt template found for '{name}'. Looked under '{workspaceRoot}' and '{_bundledRoot}'.");
    }

    private static async ValueTask<IReadOnlyList<WorkspaceFile>> ReadWorkspaceFilesAsync(
        string? workspacePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return [];
        }

        var files = ImmutableArray.CreateBuilder<WorkspaceFile>(_workspaceFileNames.Length);
        foreach (var name in _workspaceFileNames)
        {
            var path = Path.Combine(workspacePath, name);
            if (!File.Exists(path))
            {
                continue;
            }
            var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            files.Add(new WorkspaceFile(name, content));
        }
        return files.ToImmutable();
    }
}
