namespace LlamaShears.Core.Abstractions.Agent.Pipeline;

/// <summary>
/// One step in the per-batch agent onion. Implementations do work
/// before and/or after invoking the next delegate; skipping it
/// short-circuits the rest of the chain. Each implementation owns
/// exactly one concern.
/// </summary>
public interface IAgentMiddleware
{
    /// <summary>
    /// Position in the onion. Lower runs farther out. Built-in
    /// steps use <see cref="AgentMiddlewareOrder"/> (spaced 1000
    /// apart) so plugins can pick a value between them.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Runs this step around the remainder of the pipeline.
    /// </summary>
    /// <param name="context">The mutable bag for this dequeued batch.</param>
    /// <param name="next">The rest of the chain. Must be invoked unless this step short-circuits.</param>
    /// <param name="cancellationToken">Loop-level cancellation. Per-turn interrupt is <see cref="AgentPipelineContext.TurnToken"/>.</param>
    Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken);
}
