namespace LlamaShears.Core.Abstractions.Paths;

/// <summary>
/// Decides whether a workspace-relative path is protected from a
/// requested operation. Implementations evaluate the registered set
/// of <see cref="ProtectedFile"/> rules.
/// </summary>
public interface IFileProtectionPolicy
{
    /// <summary>
    /// Returns the first <see cref="ProtectedFile"/> rule whose glob
    /// (anchored at <paramref name="workspaceRoot"/>) matches
    /// <paramref name="fullPath"/>, whose <see cref="FileType"/> covers
    /// <paramref name="actualType"/>, and whose <see cref="ProtectionMode"/>
    /// includes <paramref name="requestedMode"/>; returns <see langword="null"/>
    /// when no rule applies.
    /// </summary>
    /// <param name="workspaceRoot">Absolute path to the workspace root.</param>
    /// <param name="fullPath">
    /// Absolute path of the entry under consideration. Paths inside
    /// <paramref name="workspaceRoot"/> are matched relative to it; paths
    /// outside are matched against the absolute path so policies can also
    /// protect system locations. Callers should resolve via
    /// <see cref="IPathExpander"/> before calling.
    /// </param>
    /// <param name="actualType">
    /// Entry kind of the path on disk, or the kind that would be created
    /// by a write/append.
    /// </param>
    /// <param name="requestedMode">Mode the caller is about to perform.</param>
    ProtectedFile? Match(string workspaceRoot, string fullPath, FileType actualType, ProtectionMode requestedMode);
}