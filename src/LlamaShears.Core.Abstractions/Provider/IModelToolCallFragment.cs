namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Streaming fragment carrying one tool-call request from the model.
/// Aggregating the calls from every fragment in arrival order yields
/// the full set of tools the model wants invoked before it can produce
/// its next assistant turn.
/// </summary>
public interface IModelToolCallFragment : IModelResponseFragment
{
    /// <summary>The tool the model is asking the host to invoke.</summary>
    ToolCall Call { get; }
}
