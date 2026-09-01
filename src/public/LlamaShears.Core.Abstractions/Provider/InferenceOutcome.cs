using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Aggregated result of one <see cref="IInferenceRunner.RunAsync"/>
/// pass: the streamed thought/text, any tool calls the model emitted,
/// and the cumulative token count if the provider reported it.
/// The runner does not dispatch tools — callers do.
/// </summary>
/// <param name="Thinking">Concatenated thought stream (empty when the model produced no thoughts).</param>
/// <param name="Content">Concatenated assistant content (empty when the call only produced tool calls).</param>
/// <param name="TokenCount">Cumulative token count reported via <see cref="IModelCompletionResponse.TokenCount"/>; <see langword="null"/> when the provider did not surface one.</param>
/// <param name="ToolCalls">Tool calls the model emitted during this run. Empty when the model produced only text.</param>
/// <param name="Interrupted"><see langword="true"/> when the run terminated because the caller's cancellation token fired; partial fragments and turns were still published.</param>
/// <param name="Suppressed"><see langword="true"/> when the model chose to emit no output for this turn (<see cref="Sentinel.NoResponse"/>). Distinguishes intentional silence from a transient empty response — callers should not retry on a suppressed turn.</param>
public record InferenceOutcome(
    string Thinking,
    string Content,
    int? TokenCount,
    ImmutableArray<ToolCall> ToolCalls,
    bool Interrupted = false,
    bool Suppressed = false);
