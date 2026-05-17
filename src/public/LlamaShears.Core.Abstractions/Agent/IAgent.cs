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

    /// <summary>
    /// Cancels the in-flight turn (model inference, eager tool dispatch)
    /// without affecting the agent's lifetime. The agent's persisted
    /// context up to the interrupt is preserved; partial assistant text
    /// or thought fragments are dropped from persisted history (no
    /// <c>ModelTurn</c> is recorded for the canceled turn). Live
    /// subscribers (e.g. UI streaming bubbles) may have already
    /// observed fragment events emitted before cancellation was
    /// noticed; they're responsible for closing those streams on the
    /// post-interrupt signal. Idempotent — calling when no turn is in
    /// flight is a no-op.
    /// </summary>
    Task InterruptAsync(CancellationToken cancellationToken);
}
