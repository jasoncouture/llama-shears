using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Templating;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core.PromptContext;

public sealed class FilesystemPromptContextProvider : IPromptContextProvider
{
    private const string DefaultName = "PROMPT";
    private const string TemplateExtension = ".md";
    private const string WorkspaceContextSubpath = "workspace/system/context";
    private const string DefaultBundledSubpath = "content/templates/workspace/system/context";

    // Conventional workspace files surfaced into every prompt-context
    // render. Order matters: the template renders Files in this order,
    // so BOOTSTRAP comes first when present (it's a one-shot the agent
    // is expected to act on), then identity, then soul.
    private static readonly ImmutableArray<string> _workspaceFileNames =
    [
        "BOOTSTRAP.md",
        "IDENTITY.md",
        "SOUL.md",
    ];

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
        PromptContextParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var name = string.IsNullOrWhiteSpace(templateName) ? DefaultName : templateName;
        if (name.AsSpan().IndexOfAny('/', '\\') >= 0)
        {
            throw new ArgumentException(
                $"Prompt-context template name must not contain path separators (got '{templateName}').",
                nameof(templateName));
        }

        var enriched = parameters with
        {
            Files = await ReadWorkspaceFilesAsync(parameters.WorkspacePath, cancellationToken)
                .ConfigureAwait(false),
            AdditionalFiles = ListAdditionalMarkdownFiles(parameters.WorkspacePath),
        };

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
            var rendered = await _renderer.RenderAsync(path, enriched, cancellationToken).ConfigureAwait(false);
            if (rendered is not null)
            {
                return rendered;
            }
        }
        return null;
    }

    // Surface the rest of the workspace's root markdown as a name-only
    // index. The model gets a directory listing it can pull on demand
    // via the read-file tool — without paying the token cost of the
    // bodies up front. The conventionals in `Files` are excluded so we
    // don't list a file whose contents we already injected.
    private static IReadOnlyList<string> ListAdditionalMarkdownFiles(string? workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            return [];
        }

        var conventionals = _workspaceFileNames.ToImmutableHashSet(StringComparer.Ordinal);
        var names = ImmutableArray.CreateBuilder<string>();
        foreach (var path in Directory.EnumerateFiles(workspacePath, "*.md", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(path);
            if (conventionals.Contains(name))
            {
                continue;
            }
            names.Add(name);
        }
        names.Sort(StringComparer.Ordinal);
        return names.ToImmutable();
    }

    private static async ValueTask<IReadOnlyList<PromptContextFile>> ReadWorkspaceFilesAsync(
        string? workspacePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return [];
        }

        var files = ImmutableArray.CreateBuilder<PromptContextFile>(_workspaceFileNames.Length);
        foreach (var name in _workspaceFileNames)
        {
            var path = Path.Combine(workspacePath, name);
            if (!File.Exists(path))
            {
                continue;
            }
            var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            files.Add(new PromptContextFile(name, content));
        }
        return files.ToImmutable();
    }
}
