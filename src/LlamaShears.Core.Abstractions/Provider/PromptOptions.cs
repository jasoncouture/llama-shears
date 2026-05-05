using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Context;

namespace LlamaShears.Core.Abstractions.Provider;

public sealed record PromptOptions(
    int? TokenLimit = null,
    ImmutableArray<ToolGroup> Tools = default
);
