using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Abstractions;

/// <summary>
/// An agent: an autonomous component that periodically heartbeats and
/// can be conversed with via streaming chat.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Performs the agent's periodic heartbeat tick.
    /// </summary>
    Task HeartbeatAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Streams a chat response to the supplied prompt.
    /// </summary>
    IAsyncEnumerable<IModelResponseFragment> ChatAsync(ModelPrompt prompt, CancellationToken cancellationToken);
}
