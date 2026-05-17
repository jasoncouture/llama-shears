namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Writes the active <see cref="AgentState"/> into the current data
/// context scope. Centralizes the construction so every caller stamps
/// the same shape (channel, event id, correlation id).
/// </summary>
public interface IAgentStateTracker
{
    /// <summary>
    /// Stashes a fresh <see cref="AgentState"/> on the current data
    /// context scope under <see cref="AgentState.DataKey"/>. When
    /// <paramref name="eventId"/> is <see langword="null"/>, the active
    /// <see cref="AgentConfig.Id"/> is used so the common agent-turn
    /// path doesn't need to repeat it. When <paramref name="correlationId"/>
    /// is <see langword="null"/>, a new <c>Guid.CreateVersion7</c> is
    /// minted.
    /// </summary>
    void SetState(string channelId, string? eventId = null, Guid? correlationId = null);
}
