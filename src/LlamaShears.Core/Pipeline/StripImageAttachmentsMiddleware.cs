using LlamaShears.Core.Abstractions.Agent.Pipeline;

namespace LlamaShears.Core.Pipeline;

public sealed class StripImageAttachmentsMiddleware : IAgentMiddleware
{
    /// <inheritdoc />
    public int Order => AgentMiddlewareOrder.StripImageAttachments;

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        try
        {
            await next.Invoke(context, cancellationToken);
        }
        finally
        {
            context.AgentContext.StripImageAttachments();
        }
    }
}
