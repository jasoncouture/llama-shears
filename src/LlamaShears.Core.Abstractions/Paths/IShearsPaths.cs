namespace LlamaShears.Core.Abstractions.Paths;

/// <summary>
/// Resolves on-disk paths for the well-known categories of host state
/// (<see cref="PathKind"/>). Implementations decide where each root
/// lives and whether to create directories on demand.
/// </summary>
public interface IShearsPaths
{
    /// <summary>
    /// Returns the absolute path for <paramref name="kind"/>, optionally
    /// joined with <paramref name="subpath"/>. When
    /// <paramref name="ensureExists"/> is <see langword="true"/>, the
    /// resulting directory is created if missing.
    /// </summary>
    string GetPath(PathKind kind, string? subpath = null, bool ensureExists = false);
}
