using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Provider;

public record InferenceOutcome(
    string Thinking,
    string Content,
    int? TokenCount,
    ImmutableArray<ToolCall> ToolCalls,
    ImmutableArray<ToolCallResult> ToolResults);
