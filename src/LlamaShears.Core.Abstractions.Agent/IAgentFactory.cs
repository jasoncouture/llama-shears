namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Surfaces the catalog of agents a host knows about and constructs
/// <see cref="IAgent"/> instances from <see cref="AgentConfiguration"/>.
/// Implementations decide where the catalog comes from (disk, registry,
/// in-memory) and what construction means (DI activation, plugin
/// resolution).
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    /// Lists every agent the factory surfaces, with metadata.
    /// </summary>
    IAsyncEnumerable<AgentInfo> ListAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an agent instance from <paramref name="configuration"/>.
    /// </summary>
    IAgent CreateAgent(AgentConfiguration configuration);
}
