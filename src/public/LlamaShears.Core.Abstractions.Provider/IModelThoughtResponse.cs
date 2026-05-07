namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Streaming fragment carrying hidden reasoning from a thinking-capable
/// model. Recorded for visibility but never resubmitted as part of a
/// later prompt.
/// </summary>
public interface IModelThoughtResponse : IModelResponseFragment
{
    /// <summary>
    /// Reasoning content streamed by a thinking-capable model. Thoughts
    /// are produced separately from the user-facing response and are
    /// kept out of subsequent prompts — providers must filter
    /// <see cref="ModelRole.Thought"/> turns when sending context back
    /// to the model.
    /// </summary>
    string Content { get; }
}
