using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Api.Web.Services;

/// <summary>
/// UI-side view onto the set of currently loaded agents. Lives behind
/// an interface so the Razor library does not need to reference
/// <c>Agent.Core</c>; the implementation in <c>LlamaShears.Api</c> wraps
/// the agent manager.
/// </summary>
public interface IAgentDirectory
{
    /// <summary>
    /// Snapshot of agent ids known at the moment of the call. Empty if
    /// the agent manager has not yet completed its first scan.
    /// </summary>
    IReadOnlyList<string> ListAgentIds();

    /// <summary>
    /// Snapshot of the agent's persisted conversation turns, in arrival
    /// order, so a fresh circuit can show prior conversation context to
    /// the user instead of starting blank after a refresh.
    /// </summary>
    Task<IReadOnlyList<ModelTurn>> GetTurnsAsync(string agentId, CancellationToken cancellationToken);
}
