using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.SystemPrompt;

/// <summary>
/// Per-agent workspace overlay: the absolute path the agent reads, writes,
/// and persists state in, together with the workspace files loaded at scope
/// initialization. Stashed on the data-context scope under
/// <see cref="DataKey"/> for template consumption.
/// </summary>
/// <param name="Path">Absolute workspace path.</param>
/// <param name="Files">Files loaded from the workspace at scope-init time.</param>
public sealed record WorkspaceContext(string Path, ImmutableArray<WorkspaceFile> Files)
{
    /// <summary>Key used to stash the active <see cref="WorkspaceContext"/> in the per-turn data context scope.</summary>
    public const string DataKey = "workspace";
}
