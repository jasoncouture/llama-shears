using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Payload for <see cref="Event.WellKnown.Command.AgentStop"/>. The target session
/// shuts itself down — cancels its loop, awaits drain, publishes its own
/// <c>agent:stopped</c> lifecycle event.
/// </summary>
/// <param name="SessionId">The specific agent boot to shut down. Subscribers match this against their own <see cref="SessionId"/> and ignore otherwise. If SessionId is null, it's a broadcast stop command</param>
public sealed record AgentStopRequest(SessionId? SessionId);
