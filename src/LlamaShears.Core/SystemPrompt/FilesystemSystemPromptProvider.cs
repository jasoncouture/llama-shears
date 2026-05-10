using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.SystemPrompt;

namespace LlamaShears.Core.SystemPrompt;

public sealed class FilesystemSystemPromptProvider : ISystemPromptProvider
{
    private const string DefaultFileName = "DEFAULT.md";
    private const string WorkspacePathKey = "workspace_path";
    private const string FilesKey = "files";

    private static readonly ImmutableArray<string> _workspaceFileNames =
    [
        "AGENTS.md",
        "BOOTSTRAP.md",
        "IDENTITY.md",
        "SOUL.md",
        "USER.md"
    ];

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

        var workspacePath = data.TryGetValue(WorkspacePathKey, out var raw) ? raw as string : null;
        var enriched = new Dictionary<string, object?>(data, StringComparer.OrdinalIgnoreCase)
        {
            [FilesKey] = await ReadWorkspaceFilesAsync(workspacePath, cancellationToken).ConfigureAwait(false),
        };

        var resolved = _locator.Locate(subFolder: null, fileName, DefaultFileName)
            ?? throw new FileNotFoundException(
                $"No system prompt template found for '{fileName}' across workspace, templates, or bundled roots.");

        var rendered = await _renderer.RenderAsync(resolved, enriched, cancellationToken).ConfigureAwait(false)
            ?? throw new FileNotFoundException(
                $"System prompt template '{resolved}' could not be rendered.");
        return rendered;
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
            var content = await File.ReadAllTextAsync(path, cancellationToken);
            files.Add(new WorkspaceFile(name, content));
        }
        return files.ToImmutable();
    }
}
