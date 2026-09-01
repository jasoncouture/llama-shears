using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;

namespace LlamaShears.Core.Pipeline;

public sealed class RunIterationMiddleware : IAgentMiddleware
{
    private readonly IAgentIterationRunner _iterationRunner;

    public RunIterationMiddleware(IAgentIterationRunner iterationRunner)
    {
        _iterationRunner = iterationRunner;
    }

    /// <inheritdoc />
    public int Order => AgentMiddlewareOrder.RunIteration;

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        context.Outcome = await _iterationRunner.RunAsync(
            context.AgentContext,
            context.Batch,
            context.CorrelationId,
            context.ShutdownToken,
            context.TurnToken);
        await next.Invoke(context, cancellationToken);
    }
}
