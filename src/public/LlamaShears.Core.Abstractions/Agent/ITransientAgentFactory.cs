using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Creates a transient agent rooted as a child of the caller's current
/// session. Resolves the parent <see cref="SessionPath"/> from
/// the ambient data context scope and stamps the supplied initial turn
/// onto the new agent's data so it boots straight into work.
/// </summary>
public interface ITransientAgentFactory
{
    /// <summary>
    /// Builds an <see cref="AgentHandle"/> for an <see cref="ITransientAgent"/>
    /// running under <paramref name="config"/>. The new session is a
    /// child of the caller's session and <strong>keeps</strong>
    /// <paramref name="session"/>'s canonical id (including its
    /// <see cref="SessionId.Id"/>). Callers that subscribe to
    /// <c>agent:idle</c> / <c>agent:turn</c> before spawn depend on
    /// that identity. <paramref name="initialTurn"/> is appended to
    /// <paramref name="data"/> under <see cref="TransientAgentInitialPrompt.DataKey"/>.
    /// </summary>
    /// <param name="config">Agent config for the transient agent.</param>
    /// <param name="session">Child session id. Must not be the default session; <see cref="SessionId.AgentId"/> must match <paramref name="config"/>.</param>
    /// <param name="initialTurn">First turn the transient agent will see; must have <see cref="ModelTurn.Role"/> = <see cref="ModelRole.User"/>.</param>
    /// <param name="data">Additional data scope entries to seed the child.</param>
    /// <param name="cancellationToken">Cancellation for the underlying build pipeline.</param>
    ValueTask<AgentHandle> CreateTransientAgent(
        AgentConfig config,
        SessionId session,
        ModelTurn initialTurn,
        IEnumerable<KeyValuePair<string, object?>> data,
        CancellationToken cancellationToken);
}
