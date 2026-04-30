namespace LlamaShears.Core.Abstractions.Context;

public sealed record ToolParameter(
    string Name,
    string Description,
    string Type,
    bool Required);
