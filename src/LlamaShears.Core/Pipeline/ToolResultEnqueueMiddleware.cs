using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Core.Pipeline;

public sealed class ToolResultEnqueueMiddleware : IAgentMiddleware
{
    private readonly ISessionQueue _sessionQueue;

    public ToolResultEnqueueMiddleware(ISessionQueue sessionQueue)
    {
        _sessionQueue = sessionQueue;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        await next.Invoke(context, cancellationToken);
        if (context.Outcome is not { Interrupted: false } outcome)
        {
            return;
        }

        foreach (var toolTurn in outcome.ToolResultTurns)
        {
            await _sessionQueue.EnqueueAsync(toolTurn, context.ShutdownToken);
        }
    }
}
