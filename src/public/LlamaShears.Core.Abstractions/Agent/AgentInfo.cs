using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Lightweight catalog entry describing a known agent session: enough
/// metadata to render an agent in a list or pick one for routing
/// without loading the full <see cref="AgentConfig"/>.
/// </summary>
/// <param name="Session">Session this entry identifies.</param>
/// <param name="ModelId">Globally unique identifier of the language model the agent is wired to.</param>
/// <param name="ContextWindowSize">Token budget the agent's model exposes for a single turn.</param>
/// <param name="Parameters">Free-form metadata surfaced by the producer; <see langword="null"/> = none.</param>
public record AgentInfo(
    SessionId Session,
    CompositeIdentity ModelId,
    int ContextWindowSize,
    IReadOnlyDictionary<string, object>? Parameters = null)
{
    /// <summary>Convenience accessor for <see cref="SessionId.AgentId"/>.</summary>
    public string AgentId => Session.AgentId;
}
