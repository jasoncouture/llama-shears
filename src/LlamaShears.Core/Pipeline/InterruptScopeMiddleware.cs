using LlamaShears.Core.Abstractions.Agent.Pipeline;

namespace LlamaShears.Core.Pipeline;

public sealed class InterruptScopeMiddleware : IAgentMiddleware
{
    private readonly IActiveTurnCancellation _activeTurn;

    public InterruptScopeMiddleware(IActiveTurnCancellation activeTurn)
    {
        _activeTurn = activeTurn;
    }

    /// <inheritdoc />
    public int Order => AgentMiddlewareOrder.InterruptScope;

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        using var turnCancellation = CancellationTokenSource.CreateLinkedTokenSource(context.ShutdownToken);
        context.TurnToken = turnCancellation.Token;
        _activeTurn.Register(turnCancellation);
        try
        {
            await next.Invoke(context, cancellationToken);
        }
        finally
        {
            _activeTurn.Unregister(turnCancellation);
        }
    }
}
