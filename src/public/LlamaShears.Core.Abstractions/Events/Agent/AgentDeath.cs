namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Singleton payload for <see cref="Event.WellKnown.Lifecycle.Death"/>. The agent id lives
/// on the envelope's <c>EventType.Id</c>; the payload carries no further data.
/// </summary>
public sealed record AgentDeath
{
    /// <summary>Singleton instance — subscribers never need a distinct instance per event.</summary>
    public static AgentDeath Instance { get; } = new AgentDeath();

    private AgentDeath()
    {
    }
}
