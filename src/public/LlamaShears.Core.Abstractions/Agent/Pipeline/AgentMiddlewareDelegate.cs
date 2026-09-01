namespace LlamaShears.Core.Abstractions.Agent.Pipeline;

/// <summary>
/// The remainder of the agent-turn onion: either the next
/// <see cref="IAgentMiddleware"/> or the no-op terminal.
/// First-registered middleware is outermost and sees the batch before
/// every later step; last-registered is innermost and sits next to
/// the terminal.
/// </summary>
/// <param name="context">The mutable bag for this dequeued batch.</param>
/// <param name="cancellationToken">
/// Loop-level cancellation (typically <see cref="IAgentLifetime.Stopping"/>).
/// Per-turn interrupt uses <see cref="AgentPipelineContext.TurnToken"/>,
/// not this token.
/// </param>
public delegate Task AgentMiddlewareDelegate(
    AgentPipelineContext context,
    CancellationToken cancellationToken);
