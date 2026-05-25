using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Spawns a transient sub-agent rooted under the caller's session and returns
/// the new child <see cref="SessionId"/> on success.
/// </summary>
public interface ITransientAgentSpawner
{
    /// <summary>
    /// Creates a transient child agent under <paramref name="sessionId"/> and
    /// seeds its context with the supplied turns.
    /// </summary>
    /// <param name="sessionId">Parent session id.</param>
    /// <param name="config">Agent config the child runs under.</param>
    /// <param name="turns">Initial turns to seed the child's context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new child <see cref="SessionId"/>.</returns>
    ValueTask<SessionId> SpawnAgentAsync(SessionId sessionId, AgentConfig config, IEnumerable<ModelTurn> turns, CancellationToken cancellationToken);
}
