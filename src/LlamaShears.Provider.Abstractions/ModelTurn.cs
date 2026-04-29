namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// A single turn in a conversation with a language model.
/// </summary>
public record ModelTurn(ModelRole Role, string Content);
