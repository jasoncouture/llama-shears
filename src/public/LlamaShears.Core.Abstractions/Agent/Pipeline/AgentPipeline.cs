namespace LlamaShears.Core.Abstractions.Agent.Pipeline;

/// <summary>
/// Folds <see cref="IAgentMiddleware"/> registrations into an onion.
/// First enumerated step is outermost; last is innermost, wrapping a
/// no-op terminal.
/// </summary>
public sealed class AgentPipeline : IAgentPipeline
{
    private readonly AgentMiddlewareDelegate _pipeline;

    /// <summary>
    /// Composes <paramref name="middleware"/> in enumeration order
    /// (typically DI registration order).
    /// </summary>
    /// <param name="middleware">Steps to fold. Empty is a no-op pipeline.</param>
    public AgentPipeline(IEnumerable<IAgentMiddleware> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        IReadOnlyList<IAgentMiddleware> steps = [.. middleware];
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
