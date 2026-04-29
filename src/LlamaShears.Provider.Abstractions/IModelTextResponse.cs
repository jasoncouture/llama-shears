namespace LlamaShears.Provider.Abstractions;

public interface IModelTextResponse : IModelResponseFragment
{
    /// <summary>
    /// Textual content delivered in this fragment. May be empty when
    /// the fragment carries only stream-control state (for example, a
    /// final fragment with <see cref="IsDone"/> set).
    /// </summary>
    string Content { get; }

    /// <summary>
    /// <see langword="true"/> when this is the final fragment of the
    /// response stream and no further content will follow.
    /// </summary>
    bool IsDone { get; }
}
