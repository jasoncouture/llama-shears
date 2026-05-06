namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Empty payload for <see cref="Event.WellKnown.Agent.CompactingStarted"/>
/// / <see cref="Event.WellKnown.Agent.CompactingFinished"/>. Carries
/// no data — its presence on the bus is the signal.
/// </summary>
public sealed record AgentCompactionMarker;
