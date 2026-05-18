namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Per-turn agent state surfaced in the data context. Top-level keys
/// in the data context are objects, not primitives, so anything an
/// agent wants to expose to templates or downstream consumers rides
/// under this single record.
/// </summary>
/// <param name="ChannelId">The channel the work in progress is running on (e.g. the channel a user message arrived on, or a synthetic name for non-channel work like <c>compactor</c>).</param>
/// <param name="EventId">The well-known event id stamped on outgoing fragments for this turn.</param>
/// <param name="CorrelationId">Correlation id shared by every fragment/event emitted during this turn.</param>
public sealed record AgentState(
    string ChannelId,
    string EventId,
    Guid CorrelationId)
{
    /// <summary>Key used to stash the active <see cref="AgentState"/> in the data-context scope.</summary>
    public const string DataKey = "agent_state";

    /// <summary>
    /// Session id for the work in progress; <see langword="null"/> on
    /// the agent's default (main) session. Non-default sessions
    /// (ephemeral or any future durable secondary session) set this so
    /// turns and tool results land under the right per-session persistence
    /// path.
    /// </summary>
    public Guid? SessionId { get; init; }
}
