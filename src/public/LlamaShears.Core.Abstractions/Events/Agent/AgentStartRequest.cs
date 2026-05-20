using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Payload for <see cref="Event.WellKnown.Command.AgentStart"/>. Hands a cold
/// <see cref="AgentHandle"/> built by <c>IAgentFactory</c> off to the host, which is responsible
/// for registering it in the repository and starting its loop.
/// </summary>
/// <param name="Handle">The cold handle to start.</param>
public sealed record AgentStartRequest(AgentHandle Handle);
