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
