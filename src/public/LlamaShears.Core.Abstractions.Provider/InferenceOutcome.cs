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
/// <param name="ToolResults">Results of dispatching <paramref name="ToolCalls"/>; aligned by index with <paramref name="ToolCalls"/>.</param>
public record InferenceOutcome(
    string Thinking,
    string Content,
    int? TokenCount,
    ImmutableArray<ToolCall> ToolCalls,
    ImmutableArray<ToolCallResult> ToolResults);
