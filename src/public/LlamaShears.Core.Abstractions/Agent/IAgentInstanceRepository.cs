using System.Diagnostics.CodeAnalysis;
using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Core;

/// <summary>
/// Tracks every <see cref="AgentHandle"/> across the host, keyed by session id, with knowledge
/// of the parent/child graph. Enumerations expose handles in safe disposal order.
/// </summary>
public interface IAgentInstanceRepository
{
    /// <summary>
    /// Gets an <see cref="AgentHandle"/> by its id.
    /// </summary>
    /// <param name="id">The id to find.</param>
    /// <param name="handle">The found agent handle, not null if the function returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if the handle was found, <see langword="false"/> otherwise.</returns>
    bool TryGetAgent(Guid id, [NotNullWhen(true)] out AgentHandle? handle);

    /// <summary>
    /// Removes the handle with id <paramref name="id"/>. Throws if the handle still has children.
    /// </summary>
    /// <param name="id">Id of the handle to remove.</param>
    /// <param name="handle">The removed handle, not null when the function returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when removed.</returns>
    /// <exception cref="InvalidOperationException">Thrown when descendants exist and must be removed first.</exception>
    bool Remove(Guid id, [NotNullWhen(true)] out AgentHandle? handle);

    /// <summary>
    /// Removes the handle only when it has no descendants. Returns <see langword="false"/> when descendants remain.
    /// </summary>
    bool TryRemove(Guid id, [NotNullWhen(true)] out AgentHandle? handle);

    /// <summary>
    /// Returns the ids of every tracked instance whose session name matches <paramref name="name"/>
    /// (case-insensitive).
    /// </summary>
    IEnumerable<Guid> GetAgentInstancesByName(string name);

    /// <summary>
    /// Gets an <see cref="AgentHandle"/> by its id.
    /// </summary>
    /// <param name="id">The id of the agent to get.</param>
    /// <returns>The agent.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the id is not found.</exception>
    AgentHandle GetAgent(Guid id);

    /// <summary>
    /// Adds an agent to the repository.
    /// </summary>
    /// <param name="handle">The <see cref="AgentHandle"/> to add. The dictionary key is taken from <see cref="SessionPath"/>'s id property.</param>
    /// <exception cref="InvalidOperationException">Thrown when the handle's session id is already present, or when its parent/root is unknown.</exception>
    void AddAgent(AgentHandle handle);

    /// <summary>
    /// Gets all handles.
    /// </summary>
    /// <returns>Enumerable of agent handles; ordering is safe for disposal.</returns>
    IEnumerable<AgentHandle> GetAllAgents();

    /// <summary>
    /// Returns all descendants of <paramref name="parentId"/>, with outermost leaves first —
    /// a safe stop/dispose order.
    /// </summary>
    /// <param name="parentId">The parent whose children to enumerate.</param>
    /// <returns>Children in disposal/stop order.</returns>
    IEnumerable<AgentHandle> DescendentsOf(Guid parentId);

    /// <summary>
    /// Removes all descendants of <paramref name="parentId"/>, optionally including the parent itself.
    /// </summary>
    /// <param name="parentId">Parent whose subtree to remove.</param>
    /// <param name="includeParent"><see langword="true"/> to remove the parent as well, <see langword="false"/> for children only.</param>
    void RemoveDescendents(Guid parentId, bool includeParent = true);
}
