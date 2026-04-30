using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Context;

public sealed record LanguageModelContext(
    ImmutableArray<ModelTurn> Turns,
    ImmutableArray<IContextEntry> Entries);
