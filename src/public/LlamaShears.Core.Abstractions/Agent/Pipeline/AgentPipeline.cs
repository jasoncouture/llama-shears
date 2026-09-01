namespace LlamaShears.Core.Abstractions.Agent.Pipeline;

/// <summary>
/// Folds <see cref="IAgentMiddleware"/> into an onion by
/// <see cref="IAgentMiddleware.Order"/>. Lowest is outermost; last
/// after the sort is innermost, wrapping a no-op terminal. Equal
/// orders keep their original enumeration order.
/// </summary>
public sealed class AgentPipeline : IAgentPipeline
{
    private readonly AgentMiddlewareDelegate _pipeline;

    /// <summary>
    /// Composes <paramref name="middleware"/> by
    /// <see cref="IAgentMiddleware.Order"/> (lowest outermost).
    /// </summary>
    /// <param name="middleware">Steps to fold. Empty is a no-op pipeline.</param>
    public AgentPipeline(IEnumerable<IAgentMiddleware> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        IReadOnlyList<IAgentMiddleware> steps =
        [
            .. middleware
                .Select((step, index) => (step, index))
                .OrderBy(item => item.step.Order)
                .ThenBy(item => item.index)
                .Select(item => item.step),
        ];
        AgentMiddlewareDelegate pipeline = static (_, _) => Task.CompletedTask;
        for (var i = steps.Count - 1; i >= 0; i--)
        {
            var current = steps[i];
            var next = pipeline;
            pipeline = (context, cancellationToken) => current.InvokeAsync(context, next, cancellationToken);
        }

        _pipeline = pipeline;
    }

    /// <inheritdoc />
    public Task InvokeAsync(AgentPipelineContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _pipeline.Invoke(context, cancellationToken);
    }
}
