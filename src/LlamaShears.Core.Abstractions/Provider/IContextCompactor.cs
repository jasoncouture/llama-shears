namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Decides whether a <see cref="ModelPrompt"/> exceeds the model's
/// context window and, if so, rewrites it so the next model call
/// fits — typically by summarizing earlier turns into a single
/// assistant message and preserving the trailing user turn. Pure
/// w.r.t. external storage; callers archive any displaced context
/// themselves.
/// </summary>
public interface IContextCompactor
{
    /// <summary>
    /// Returns <paramref name="prompt"/> unchanged when no compaction
    /// is needed (under budget, too few turns, or no context window
    /// known). Otherwise returns a rebuilt prompt; reference equality
    /// with the input is the caller's signal that compaction did or
    /// did not occur.
    /// </summary>
    ValueTask<ModelPrompt> CompactAsync(
        ModelPrompt prompt,
        ILanguageModel model,
        ModelConfiguration configuration,
        CancellationToken cancellationToken);
}
