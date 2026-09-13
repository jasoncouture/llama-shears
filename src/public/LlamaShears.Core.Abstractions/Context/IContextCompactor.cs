using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Decides whether a <see cref="ModelPrompt"/> needs compaction and
/// commits a summary plus the preserved tail. Does not call the model
/// and does not render system or ephemeral templates; the pipeline
/// overlays those template names and <c>next</c>s so existing
/// middleware render them.
/// </summary>
public interface IContextCompactor
{
    /// <summary>
    /// Returns a <see cref="CompactionPlan"/> when older history should
    /// be summarized, or <see langword="null"/> when no compaction is
    /// needed (under budget, six or fewer eligible turns, or no context
    /// window known). Pass <paramref name="force"/> as
    /// <see langword="true"/> to skip the under-budget guard.
    /// </summary>
    ValueTask<CompactionPlan?> TryPrepareAsync(
        AgentContext agentContext,
        ModelPrompt prompt,
        bool force,
        CancellationToken cancellationToken);

    /// <summary>
    /// Archives the live context and writes
    /// <c>[system?, Assistant(summary), ...preserved]</c>.
    /// </summary>
    ValueTask<ModelPrompt> CommitAsync(
        CompactionPlan plan,
        string summary,
        CancellationToken cancellationToken);
}
