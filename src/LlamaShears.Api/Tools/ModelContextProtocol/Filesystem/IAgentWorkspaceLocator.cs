namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

/// <summary>
/// Resolves the workspace root for the agent currently bound to the
/// request, used by filesystem tools to anchor relative paths and to
/// enforce the workspace boundary on writes.
/// </summary>
public interface IAgentWorkspaceLocator
{
    /// <summary>
    /// Returns the current agent's workspace context. Falls back to
    /// <see cref="Environment.CurrentDirectory"/> when no authenticated
    /// agent is on the request, or when the agent's config does not
    /// declare a workspace path.
    /// </summary>
    Task<AgentWorkspace> GetAsync(CancellationToken cancellationToken);
}
