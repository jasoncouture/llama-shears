namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Policy + name-list pair used by <see cref="AgentBehaviorOptions"/> to control
/// which skills, MCP sources, or MCP tools the agent is allowed to use.
/// The interpretation of <see cref="Names"/> depends on <see cref="Default"/>:
/// <see cref="FilterPolicy.Allow"/> treats <see cref="Names"/> as an allowlist,
/// <see cref="FilterPolicy.Deny"/> treats it as a denylist,
/// <see cref="FilterPolicy.Default"/> ignores it and lets everything through,
/// and <see cref="FilterPolicy.Disable"/> blocks the entire subsystem.
/// </summary>
/// <param name="Names">Names the policy operates on. Treated as an allowlist or denylist depending on <paramref name="Default"/>.</param>
/// <param name="Default">Policy applied to <paramref name="Names"/> and everything outside it.</param>
public sealed record FilterOptions(HashSet<string> Names, FilterPolicy Default);
