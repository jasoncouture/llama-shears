using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Snapshot of the agent's conversation with the language model: the
/// ordered turns the model has seen plus the full polymorphic entry log
/// that backs them. This is the LLM-facing slice of the context, not
/// the runtime envelope as a whole — see <see cref="AgentContext"/>.
/// </summary>
public sealed record LanguageModelContext(
    ImmutableArray<ModelTurn> Turns,
    ImmutableArray<IContextEntry> Entries);
