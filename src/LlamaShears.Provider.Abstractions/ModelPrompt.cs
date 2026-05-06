namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// A prompt sent to a language model, expressed as the conversation
/// turns leading up to and including the user turn being answered.
/// </summary>
/// <param name="Turns">
/// Ordered turns in the conversation. The final turn is the user turn
/// that the model is being prompted to respond to.
/// </param>
public record ModelPrompt(IReadOnlyList<ModelTurn> Turns);
