namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// An autonomous component that ingests input turns, drives a model,
/// and produces output turns. Identity, heartbeat cadence, channels,
/// and conversation state are internal and reachable through the
/// services that own the agent (config provider, context store,
/// message bus).
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Starts the agent's run loop. Idempotent at construction time —
    /// invoking it twice on the same instance throws. The owner (the
    /// agent manager) calls this once after the agent's scope is built;
    /// shutdown happens through scope disposal.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);
}
