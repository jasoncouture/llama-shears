namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Output of dispatching a single <see cref="ToolCall"/> back to the
/// model.
/// </summary>
/// <param name="Content">String body fed back into the conversation.</param>
/// <param name="IsError">Whether the tool reported a failure; the agent loop uses this to decide whether to surface the error to the model.</param>
public sealed record ToolCallResult(string Content, bool IsError);
