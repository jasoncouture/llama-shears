namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Read-only view onto the set of agents currently loaded by the
/// host. Consumers can list agent ids and check whether a given id
/// resolves to a loaded agent. The lifecycle (loading/unloading,
/// reconciliation) is owned by the implementation and not part of
/// this surface.
/// </summary>
public interface IAgentManager
{
    /// <summary>
    /// Snapshot of every agent id currently loaded, in stable
    /// ordinal-ignore-case order.
    /// </summary>
    IReadOnlyList<string> AgentIds { get; }

    /// <summary>
    /// Returns <see langword="true"/> if an agent with the given id
    /// is currently loaded.
    /// </summary>
    bool Contains(string agentId);
}
