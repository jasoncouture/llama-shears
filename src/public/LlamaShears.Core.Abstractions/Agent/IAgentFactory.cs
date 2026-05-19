using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Core;

/// <summary>
/// Spawns a clean agent state: blank execution context, fresh DI scope, fresh keyed data context seeded with the
/// supplied <see cref="AgentConfig"/> plus any caller-supplied overlay data, eager-resolved language model, and a
/// started <see cref="IAgent"/>. Returns the <see cref="AgentHandle"/> that owns the resulting scope.
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    /// Creates a new agent handle with the specified parameters.
    /// </summary>
    /// <param name="config">Agent configuration.</param>
    /// <param name="sessionPath">Agent's unique session path.</param>
    /// <param name="data">Additional data to inject into the agent's context data scope.</param>
    /// <param name="cancellationToken">Cancellation token for the build pipeline.</param>
    /// <returns>A ready-to-start, validated <see cref="AgentHandle"/> with a unique execution context.</returns>
    ValueTask<AgentHandle> CreateAgentAsync(AgentConfig config, SessionPath sessionPath,
        IEnumerable<KeyValuePair<string, object?>> data, CancellationToken cancellationToken);
}
