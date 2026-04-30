namespace LlamaShears.Core.Abstractions.Agent;

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
