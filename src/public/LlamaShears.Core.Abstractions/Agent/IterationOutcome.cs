using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Result of running one agent iteration: was the turn interrupted before
/// completion, and any tool-result turns the inference produced that the
/// caller should feed back into its driver on the next iteration.
/// </summary>
/// <param name="Interrupted">
/// <see langword="true"/> when the turn cancellation token tripped before
/// the inference finished; partial output may have been published but the
/// caller should not act on tool results in that case.
/// </param>
/// <param name="ToolResultTurns">
/// One <see cref="ModelTurn"/> per dispatched tool call. Empty when the
/// model emitted no tool calls (the natural exit condition).
/// </param>
public sealed record IterationOutcome(
    bool Interrupted,
    ImmutableArray<ModelTurn> ToolResultTurns);
