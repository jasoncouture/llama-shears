using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Context;

public sealed record ToolDescriptor(
    string Name,
    string Description,
    ImmutableArray<ToolParameter> Parameters);
