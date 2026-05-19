namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Payload for <see cref="Event.WellKnown.Command.AgentUnload"/>. Carries
/// no data — its presence on the bus, with the agent id on
/// <see cref="EventType.Id"/>, is the signal.
/// </summary>
public sealed record AgentUnloadRequest
{
    /// <summary>Singleton marker; subscribers never need a distinct instance per event.</summary>
    public static AgentUnloadRequest Instance { get; } = new AgentUnloadRequest();

    private AgentUnloadRequest()
    {
    }
}
