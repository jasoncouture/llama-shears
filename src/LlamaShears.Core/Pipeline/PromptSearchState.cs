using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Pipeline;

internal readonly record struct PromptSearchState(
    bool UserMessageSeen,
    bool Complete,
    ImmutableArray<ModelTurn> Turns);
