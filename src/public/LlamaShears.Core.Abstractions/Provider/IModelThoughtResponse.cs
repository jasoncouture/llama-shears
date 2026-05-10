namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Streaming fragment carrying hidden reasoning from a thinking-capable
/// model.
/// </summary>
public interface IModelThoughtResponse : IModelResponseFragment
{
    /// <summary>
    /// Reasoning content streamed by a thinking-capable model.
    /// </summary>
    string Content { get; }
}
