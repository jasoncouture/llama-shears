namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Streaming fragment carrying user-visible text. Aggregating every
/// fragment's <see cref="Content"/> in arrival order yields the model's
/// final response.
/// </summary>
public interface IModelTextResponse : IModelResponseFragment
{
    /// <summary>
    /// Textual content delivered in this fragment. The provider is
    /// responsible for terminating the fragment stream when the
    /// response is complete; a "done" flag is unnecessary and absent
    /// by design.
    /// </summary>
    string Content { get; }
}
