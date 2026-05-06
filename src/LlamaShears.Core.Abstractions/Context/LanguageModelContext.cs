using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Conversation slice of an <see cref="AgentContext"/> snapshot:
/// chronological turns, the polymorphic entry log they were drawn
/// from, and the model's current context-window size in tokens.
/// </summary>
/// <param name="Turns">Conversation turns in chronological order.</param>
/// <param name="Entries">Polymorphic entry log including non-turn entries (e.g. token-info markers).</param>
/// <param name="ContextWindowTokenCount">The model's context-window size, in tokens.</param>
public sealed record LanguageModelContext(
    ImmutableArray<ModelTurn> Turns,
    ImmutableArray<IContextEntry> Entries,
    int ContextWindowTokenCount);
