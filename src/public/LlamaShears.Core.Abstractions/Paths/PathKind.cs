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
    /// <summary>
    /// The shared, user-profile-level skill root (e.g. <c>~/.agent/skills</c>).
    /// Skills installed here are visible to every host instance run by the user.
    /// </summary>
    GlobalSkills,
    /// <summary>
    /// The host-scoped skill root that lives under the framework data directory
    /// (<see cref="Data"/>). Skills installed here are shared by every agent of
    /// this host but isolated from other hosts on the machine.
    /// </summary>
    AppSkills,
    /// <summary>
    /// The per-agent skill root located inside the agent's workspace
    /// (<see cref="Workspace"/>/<c>&lt;subpath&gt;</c>/<c>skills</c>). Skills installed here are
    /// only visible to the agent whose workspace contains them.
    /// </summary>
    AgentSkills
}
