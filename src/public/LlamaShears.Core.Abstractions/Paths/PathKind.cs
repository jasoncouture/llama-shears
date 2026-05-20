namespace LlamaShears.Core.Abstractions.Paths;

/// <summary>
/// Well-known categories of host state whose on-disk root is
/// resolved by <see cref="IApplicationPathProvider"/>. Implementations decide
/// where each root lives and whether to create directories on demand.
/// </summary>
public enum PathKind
{
    /// <summary>The root for all framework data (catch-all for state that does not have a more specific kind).</summary>
    Data,
    /// <summary>The shared workspace directory, including templates and per-agent workspace overlays.</summary>
    Workspace,
    /// <summary>The directory holding per-agent <c>&lt;id&gt;.json</c> configuration files.</summary>
    Agents,
    /// <summary>The directory holding bundled and operator-supplied prompt/context templates.</summary>
    Templates,
    /// <summary>The directory holding per-agent persisted conversation logs (the "context" store).</summary>
    Context,
}
