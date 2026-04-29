namespace LlamaShears.Agent.Abstractions;

/// <summary>
/// Factory for enumerating and constructing <see cref="IAgent"/> instances.
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    /// List all agents surfaced by this factory, with metadata.
    /// </summary>
    IAsyncEnumerable<AgentInfo> ListAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an agent instance from the given configuration.
    /// </summary>
    IAgent CreateAgent(AgentConfiguration configuration);
}
