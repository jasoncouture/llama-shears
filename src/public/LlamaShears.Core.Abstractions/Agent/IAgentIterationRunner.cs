using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Runs a single agent iteration: builds the prompt from the supplied
/// context and turn batch, invokes the language model (with the
/// empty-response retry), persists the model's output via the active
/// context store, and returns any tool-result turns the caller should
/// feed back on the next iteration. Knows nothing about session queues,
/// agent locks, or interrupt subscriptions — those concerns belong to
/// the surrounding loop owner.
/// </summary>
public interface IAgentIterationRunner
{
    /// <summary>
    /// Runs one iteration. The caller is responsible for any lock
    /// acquisition, interrupt-token wiring, and acting on returned
    /// tool-result turns.
    /// </summary>
    /// <param name="context">
    /// Live context for the session being driven. Token usage and any
    /// turn the inference path persists land here.
    /// </param>
    /// <param name="batch">
    /// Input turns for this iteration (typically the freshly dequeued
    /// user/tool turns).
    /// </param>
    /// <param name="correlationId">
    /// Correlation id stamped on every event published during the
    /// iteration. Lets subscribers tie streamed fragments back to the
    /// inbound batch.
    /// </param>
    /// <param name="outerCancellationToken">
    /// Cancellation that should outlive an interrupt — used for tail
    /// persistence work that must finish even after the turn was
    /// interrupted.
    /// </param>
    /// <param name="turnCancellationToken">
    /// Cancellation linked to interrupt signals; cancelling this stops
    /// the inference itself.
    /// </param>
    Task<IterationOutcome> RunAsync(
        IAgentContext context,
        ImmutableArray<ModelTurn> batch,
        Guid correlationId,
        CancellationToken outerCancellationToken,
        CancellationToken turnCancellationToken);
}
