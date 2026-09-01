using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Pipeline;

public sealed class RunIterationMiddleware : IAgentMiddleware
{
    private readonly IAgentIterationRunner _iterationRunner;
    private readonly IDataContextScope _dataScope;

    public RunIterationMiddleware(IAgentIterationRunner iterationRunner, IDataContextScope dataScope)
    {
        _iterationRunner = iterationRunner;
        _dataScope = dataScope;
    }

    /// <inheritdoc />
    public int Order => AgentMiddlewareOrder.RunIteration;

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        context.SessionId = _dataScope.GetCurrentSessionId();
        context.Outcome = await _iterationRunner.RunAsync(context);
        await next.Invoke(context, cancellationToken);
    }
}
