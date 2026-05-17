using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Aggregated result of one <see cref="IInferenceRunner.RunAsync"/>
/// pass: the streamed thought/text, any tool calls and their replies,
/// and the cumulative token count if the provider reported it.
/// </summary>
/// <param name="Thinking">Concatenated thought stream (empty when the model produced no thoughts).</param>
/// <param name="Content">Concatenated assistant content (empty when the call only produced tool calls).</param>
/// <param name="TokenCount">Cumulative token count reported via <see cref="IModelCompletionResponse.TokenCount"/>; <see langword="null"/> when the provider did not surface one.</param>
/// <param name="ToolCalls">Tool calls the model emitted during this run.</param>
/// <param name="ToolResults">Results of dispatching <paramref name="ToolCalls"/>; aligned by index with <paramref name="ToolCalls"/>. Tool calls that were in flight when the run was interrupted carry a synthetic error result.</param>
/// <param name="Interrupted"><see langword="true"/> when the run terminated because the caller's cancellation token fired; partial fragments and turns were still published, and any in-flight tool calls were collapsed into error results so caller-side history remains paired.</param>
/// <param name="Suppressed"><see langword="true"/> when the model chose to emit no output for this turn (sentinel <c>NO_RESPONSE</c>). Distinguishes intentional silence from a transient empty response — callers should not retry on a suppressed turn.</param>
public record InferenceOutcome(
    string Thinking,
    string Content,
    int? TokenCount,
    ImmutableArray<ToolCall> ToolCalls,
    ImmutableArray<ToolCallResult> ToolResults,
    bool Interrupted = false,
    bool Suppressed = false);
