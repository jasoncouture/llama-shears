using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Prepared compaction split: the transcript the summarizer should see,
/// plus the live turns that survive after the summary lands.
/// </summary>
/// <param name="SystemTurn">
/// The original (non-compaction) system turn from the live prompt, if
/// any. Reattached when the rebuilt prompt is committed.
/// </param>
/// <param name="Transcript">Flattened older history as a single user turn.</param>
/// <param name="Preserved">Eligible turns kept after the summary.</param>
public sealed record CompactionPlan(
    ModelTurn? SystemTurn,
    ModelTurn Transcript,
    ImmutableArray<ModelTurn> Preserved);
