using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Api.Web.Services;

/// <summary>
/// UI-side view onto the set of currently loaded agents. Lives behind
/// an interface so the Razor library does not need to reference
/// <c>Agent.Core</c>; the implementation in <c>LlamaShears.Api</c> wraps
/// the agent manager.
/// <para>
/// DO NOT add new methods here. New agent-targeted commands belong on
/// the event bus (see the <c>command:*</c> event source). The methods
/// already on this interface that fan out to events are flagged
/// <see cref="ObsoleteAttribute"/> and will be removed once callers
/// publish those events directly.
/// </para>
/// </summary>
public interface IAgentDirectory
{
    /// <summary>
    /// Snapshot of agent ids known at the moment of the call. Empty if
    /// the agent manager has not yet completed its first scan.
    /// </summary>
    IReadOnlyList<string> ListAgentIds();

    /// <summary>
    /// Snapshot of <paramref name="session"/>'s persisted conversation
    /// turns, in arrival order, so a fresh circuit can show prior
    /// conversation context to the user instead of starting blank after
    /// a refresh.
    /// </summary>
    Task<IReadOnlyList<ModelTurn>> GetTurnsAsync(SessionId session, CancellationToken cancellationToken);

    /// <summary>
    /// Clears <paramref name="session"/>'s stored context. With
    /// <paramref name="archive"/> set, the existing context is moved to
    /// a timestamped archive file; otherwise it is deleted. The live
    /// <c>IAgentContext</c> is emptied either way.
    /// </summary>
    Task ClearAsync(SessionId session, bool archive, CancellationToken cancellationToken);

    /// <summary>
    /// Forces an immediate context compaction on <paramref name="session"/>
    /// regardless of token-budget pressure. The agent's processing gate
    /// is acquired for the duration so this serializes naturally with
    /// in-flight turn handling.
    /// </summary>
    [Obsolete("Publish command:compaction-request:<sessionId> with AgentCompactionRequest.Forced instead.", error: false)]
    Task RequestCompactionAsync(SessionId session, CancellationToken cancellationToken);

    /// <summary>
    /// Interrupts <paramref name="session"/>'s in-flight turn, if any.
    /// Persisted context up to the interrupt is preserved; partial
    /// assistant text or thought fragments emitted by the canceled turn
    /// are dropped. The agent stays live and resumes on the next inbound
    /// message.
    /// </summary>
    [Obsolete("Publish command:interrupt-agent:<sessionId> with AgentInterruptRequest.Instance instead.", error: false)]
    Task InterruptAsync(SessionId session, CancellationToken cancellationToken);
}
