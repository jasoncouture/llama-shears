using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.SystemPrompt;

namespace LlamaShears.Core.SystemPrompt;

internal sealed class WorkspaceContextDataProvider : IDataContextItemProvider
{
    private static readonly ImmutableArray<string> _workspaceFileNames =
    [
        "AGENTS.md",
        "BOOTSTRAP.md",
        "IDENTITY.md",
        "SOUL.md",
        "USER.md"
    ];

    private readonly IDataContextScope _scope;
    private readonly IFileParserCache<WorkspaceContextDataProvider> _cache;

    public WorkspaceContextDataProvider(
        IDataContextScope scope,
        IFileParserCache<WorkspaceContextDataProvider> cache)
    {
        _scope = scope;
        _cache = cache;
    }

    public async Task<IEnumerable<KeyValuePair<string, object?>>> GetItemsForCurrentContext(
        CancellationToken cancellationToken = default)
    {
        var workspacePath = _scope.GetAgentConfig().WorkspacePath ?? string.Empty;
        var files = await ReadWorkspaceFilesAsync(workspacePath, cancellationToken);
        var workspace = new WorkspaceContext(workspacePath, files);
        return [new KeyValuePair<string, object?>(WorkspaceContext.DataKey, workspace)];
    }

    private async ValueTask<ImmutableArray<WorkspaceFile>> ReadWorkspaceFilesAsync(
        string workspacePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return [];
        }

        var files = ImmutableArray.CreateBuilder<WorkspaceFile>();
        foreach (var name in _workspaceFileNames)
        {
            var fullPath = Path.Combine(workspacePath, name);
            var file = await _cache.GetOrParseAsync<WorkspaceFile, string>(
                fullPath,
                fullPath,
                ParseWorkspaceFileAsync,
                cancellationToken);
            if (file is not null)
            {
                files.Add(file);
            }
        }
        return files.ToImmutable();
    }

    private static async ValueTask<WorkspaceFile?> ParseWorkspaceFileAsync(
        Stream? stream,
        string fullPath,
        CancellationToken cancellationToken)
    {
        if (stream is null)
        {
            return null;
        }
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken);
        var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
        if (directory.Length > 0 && directory[^1] != Path.DirectorySeparatorChar)
        {
            directory += Path.DirectorySeparatorChar;
        }
        return new WorkspaceFile(Path.GetFileName(fullPath), directory, content);
    }
}
