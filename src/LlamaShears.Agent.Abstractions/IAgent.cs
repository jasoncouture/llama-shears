using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Abstractions;

/// <summary>
/// An agent: an autonomous component that periodically heartbeats and
/// can be conversed with via streaming chat.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Wall-clock time the agent's <see cref="HeartbeatAsync"/> last completed.
    /// </summary>
    DateTimeOffset LastHeartbeatAt { get; }

    /// <summary>
    /// Desired cadence between heartbeats. The heartbeat service polls at a
    /// fixed one-minute granularity, so periods shorter than one minute will
    /// effectively fire every minute.
    /// </summary>
    TimeSpan HeartbeatPeriod { get; }

    /// <summary>
    /// Whether heartbeats are currently enabled for this agent. The heartbeat
    /// service skips agents whose value is <see langword="false"/>.
    /// </summary>
    bool HeartbeatEnabled { get; }

    /// <summary>
    /// Performs the agent's periodic heartbeat tick.
    /// </summary>
    Task HeartbeatAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Streams a chat response to the supplied prompt.
    /// </summary>
    IAsyncEnumerable<IModelResponseFragment> ChatAsync(ModelPrompt prompt, CancellationToken cancellationToken);
}
