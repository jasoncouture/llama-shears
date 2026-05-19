namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Stable handle naming a target session — the
/// (<see cref="AgentId"/>, <see cref="SessionId"/>) pair used by an
/// ephemeral child session to address its parent (e.g. for routing the
/// reply published via <c>session_reply</c>).
/// </summary>
/// <param name="AgentId">Owning agent id.</param>
/// <param name="SessionId">
/// Session id within the agent; <see langword="null"/> identifies the
/// agent's default (main) session.
/// </param>
public sealed record EphemeralSessionReference(string AgentId, Guid? SessionId);
