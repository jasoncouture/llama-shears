namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// An autonomous component that ingests input turns, drives a model,
/// and produces output turns. Identity, heartbeat cadence, channels,
/// and conversation state are internal and reachable through the
/// services that own the agent (config provider, context store,
/// message bus).
/// </summary>
public interface IAgent : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Acquires the agent's processing gate and runs the context
    /// compactor against the agent's current context, bypassing the
    /// usual under-budget guard so the call is willing to compact a
    /// healthy-but-aged context. The compactor's other guards (min
    /// turn count, missing context length) still apply, so a small
    /// or unconfigured context is left alone.
    /// </summary>
    Task RequestCompactionAsync(CancellationToken cancellationToken);

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
