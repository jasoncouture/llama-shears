namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// A prompt expressed as the ordered conversation turns leading up to
/// and including the user turn the model is being asked to respond to.
/// </summary>
public record ModelPrompt(IReadOnlyList<ModelTurn> Turns);
