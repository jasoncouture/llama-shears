using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Payload for <see cref="Event.WellKnown.Command.AgentStop"/>. Targets a specific session that
/// the host is about to tear down; carries a non-null <see cref="SessionId"/>.
/// </summary>
/// <param name="SessionId">Session id whose teardown is being requested.</param>
public sealed record AgentStopRequest(SessionId SessionId);
