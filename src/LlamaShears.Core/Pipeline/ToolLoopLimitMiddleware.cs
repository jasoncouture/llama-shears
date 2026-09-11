using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;

namespace LlamaShears.Core.Pipeline;

public sealed class ToolLoopLimitMiddleware : IAgentMiddleware
{
    private readonly IToolLoopBudget _budget;

    public ToolLoopLimitMiddleware(IToolLoopBudget budget)
    {
        _budget = budget;
    }

    /// <inheritdoc />
    public int Order => AgentMiddlewareOrder.ToolLoopLimit;

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        if (_budget.IsFinal && context.Outcome is { ToolCalls.IsDefaultOrEmpty: false } outcome)
        {
            context.Outcome = outcome with { ToolCalls = [] };
            context.Tools = [];
        }

        await next.Invoke(context, cancellationToken);
    }
}
