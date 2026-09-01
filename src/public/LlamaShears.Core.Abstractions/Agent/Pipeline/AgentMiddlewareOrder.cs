using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Core.Abstractions.Agent.Pipeline;

/// <summary>
/// Well-known <see cref="IAgentMiddleware.Order"/> values for the
/// default onion. Lowest is outermost. Built-ins are spaced 1000
/// apart so a plugin can sit in any gap (or outside the range)
/// without colliding.
/// </summary>
public static class AgentMiddlewareOrder
{
    /// <summary>Swallow turn failures; rethrow shutdown cancellation.</summary>
    public const int TurnException = 1000;

    /// <summary>Start and dispose the <c>chat {model}</c> activity.</summary>
    public const int AgentActivity = 2000;

    /// <summary>Assign a correlation id and logger scope.</summary>
    public const int CorrelationScope = 3000;

    /// <summary>Hold <see cref="IAgentLock"/> across the rest of the turn.</summary>
    public const int AgentLock = 4000;

    /// <summary>Install the linked turn cancellation token.</summary>
    public const int InterruptScope = 5000;

    /// <summary>Re-enqueue tool-result turns when the iteration was not interrupted.</summary>
    public const int ToolResultEnqueue = 6000;

    /// <summary>Render the persistent system-prompt turn onto the bag.</summary>
    public const int SystemPrompt = 7000;

    /// <summary>Invoke <see cref="IAgentIterationRunner"/>.</summary>
    public const int RunIteration = 8000;
}
