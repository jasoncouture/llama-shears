using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Context;

public sealed record ToolContext(ImmutableArray<ToolDescriptor> Items);
