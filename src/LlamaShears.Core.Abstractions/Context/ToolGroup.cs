using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Context;

public sealed record ToolGroup(
    string Source,
    ImmutableArray<ToolDescriptor> Tools);
