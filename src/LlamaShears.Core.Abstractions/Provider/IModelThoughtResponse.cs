namespace LlamaShears.Core.Abstractions.Provider;

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
