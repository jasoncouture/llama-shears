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
    /// <summary>Starts an agent with no overlay data — convenience for the common case.</summary>
    async Task<AgentHandle> StartAgentAsync(AgentConfig config, SessionId session, CancellationToken cancellationToken)
        => await StartAgentAsync(config, session, [], cancellationToken);

    /// <summary>
    /// Starts an agent. Default globals (<see cref="AgentConfig.DataKey"/>, <c>ModelConfiguration.DataKey</c>) are
    /// written first; entries from <paramref name="data"/> overlay on top — caller wins.
    /// </summary>
    Task<AgentHandle> StartAgentAsync(AgentConfig config, SessionId session, IEnumerable<KeyValuePair<string, object?>> data, CancellationToken cancellationToken);
}
