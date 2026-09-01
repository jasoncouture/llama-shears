namespace LlamaShears.Core.Abstractions.Agent.Pipeline;

/// <summary>
/// Invokes the registered <see cref="IAgentMiddleware"/> onion for one
/// dequeued batch. The loop owner builds <see cref="AgentPipelineContext"/>
/// and calls this once per batch; it does not dequeue, lock, or subscribe.
/// </summary>
public interface IAgentPipeline
{
    /// <summary>
    /// Runs <paramref name="context"/> through every registered
    /// middleware, outermost first. The terminal is a no-op — the
    /// innermost step must perform the iteration work.
    /// </summary>
    /// <param name="context">The bag for this batch. Must not be <see langword="null"/>.</param>
    /// <param name="cancellationToken">Loop-level cancellation forwarded to every step.</param>
    Task InvokeAsync(AgentPipelineContext context, CancellationToken cancellationToken);
}
