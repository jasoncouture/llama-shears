namespace LlamaShears.Agent.Abstractions.Events;

/// <summary>
/// The kind of streaming fragment carried by an
/// <see cref="AgentFragmentEmitted"/> event.
/// </summary>
public enum AgentFragmentKind
{
    /// <summary>
    /// A fragment of the assistant's visible response text.
    /// </summary>
    Text,

    /// <summary>
    /// A fragment of the assistant's reasoning/thought stream, when the
    /// model emits one.
    /// </summary>
    Thought,
}
