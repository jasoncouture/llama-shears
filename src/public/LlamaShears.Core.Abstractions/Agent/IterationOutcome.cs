using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Result of running one agent iteration: was the turn interrupted before
/// completion, the tool calls the model emitted, and any tool-result
/// turns dispatch middleware produced for the next iteration.
/// </summary>
/// <param name="Interrupted">
/// <see langword="true"/> when the turn cancellation token tripped before
/// the inference finished; partial output may have been published.
/// Dispatch middleware still runs so in-flight calls pair with error
/// results; the enqueue step does not feed those back.
/// </param>
/// <param name="ToolResultTurns">
/// One <see cref="ModelTurn"/> per dispatched tool call. Empty until
/// dispatch middleware runs, or when the model emitted no tool calls.
/// </param>
/// <param name="ToolCalls">
/// Tool calls the model emitted. Empty when the model produced only text.
/// Dispatch middleware reads this; the iteration runner does not execute them.
/// </param>
public sealed record IterationOutcome(
    bool Interrupted,
    ImmutableArray<ModelTurn> ToolResultTurns,
    ImmutableArray<ToolCall> ToolCalls = default);
