using LlamaShears.Core.Abstractions.Agent.Pipeline;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Pipeline;

public sealed class CorrelationScopeMiddleware : IAgentMiddleware
{
    private readonly ILogger<CorrelationScopeMiddleware> _logger;

    public CorrelationScopeMiddleware(ILogger<CorrelationScopeMiddleware> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        context.CorrelationId = Guid.CreateVersion7();
        using var scope = _logger.BeginScope("{AgentTurnId}", context.CorrelationId);
        await next.Invoke(context, cancellationToken);
    }
}
