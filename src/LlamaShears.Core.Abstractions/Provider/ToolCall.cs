namespace LlamaShears.Core.Abstractions.Provider;

public sealed record ToolCall(
    string Source,
    string Name,
    string ArgumentsJson,
    string? CallId = null);
