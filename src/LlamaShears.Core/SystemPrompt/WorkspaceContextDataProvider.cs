using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
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

    public WorkspaceContextDataProvider(IDataContextScope scope)
    {
        _scope = scope;
    }

    public async Task<IEnumerable<KeyValuePair<string, object?>>> GetItemsForCurrentContext(
        CancellationToken cancellationToken = default)
    {
        var workspacePath = _scope.GetAgentConfig().WorkspacePath ?? string.Empty;
        var files = await ReadWorkspaceFilesAsync(workspacePath, cancellationToken);
        var workspace = new WorkspaceContext(workspacePath, files);
        return [new KeyValuePair<string, object?>(WorkspaceContext.DataKey, workspace)];
    }

    private static async ValueTask<ImmutableArray<WorkspaceFile>> ReadWorkspaceFilesAsync(
        string workspacePath,
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
