namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Empty payload for the agent lifecycle events
/// (<see cref="Event.WellKnown.Agent.Loaded"/>,
/// <see cref="Event.WellKnown.Agent.Unloaded"/>,
/// <see cref="Event.WellKnown.Agent.LoadError"/>).
/// Carries no data — its presence on the bus, with the agent id on
/// <see cref="EventType.Id"/>, is the signal.
/// </summary>
public sealed record AgentLifecycleMarker
{
    /// <summary>Singleton marker; subscribers never need a distinct instance per event.</summary>
    public static AgentLifecycleMarker Instance { get; } = new AgentLifecycleMarker();

    private AgentLifecycleMarker()
    {
    }
}
