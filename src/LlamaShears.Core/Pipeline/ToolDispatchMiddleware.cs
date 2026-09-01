using LlamaShears.Core.Abstractions.Agent.Pipeline;

namespace LlamaShears.Core.Pipeline;

public sealed class ToolDispatchMiddleware : IAgentMiddleware
{
    private readonly ToolCallExecutor _executor;

    public ToolDispatchMiddleware(ToolCallExecutor executor)
    {
        _executor = executor;
    }

    /// <inheritdoc />
    public int Order => AgentMiddlewareOrder.ToolDispatch;

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        if (context.Outcome is { ToolCalls.IsDefaultOrEmpty: false } outcome)
        {
            var session = context.SessionId
                ?? throw new InvalidOperationException(
                    "Run-iteration middleware must set AgentPipelineContext.SessionId before tool dispatch.");
            var channelId = context.Prompt?.Turns is { Count: > 0 } turns
                ? turns[^1].ChannelId
                : context.Batch is { IsDefaultOrEmpty: false }
                    ? context.Batch[^1].ChannelId
                    : null;
            var turnSessionId = context.Batch is { IsDefaultOrEmpty: false }
                ? context.Batch[^1].SessionId
                : null;
            var toolTurns = await _executor.ExecuteAsync(
                outcome.ToolCalls,
                context.Tools,
                session,
                context.CorrelationId,
                channelId,
                turnSessionId,
                context.TurnToken,
                context.ShutdownToken);
            context.Outcome = outcome with { ToolResultTurns = toolTurns };
        }

        await next.Invoke(context, cancellationToken);
    }
}
