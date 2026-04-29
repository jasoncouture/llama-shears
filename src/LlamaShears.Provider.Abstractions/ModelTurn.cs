namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// A single turn in a conversation with a language model.
/// </summary>
/// <param name="Role">Who authored the turn.</param>
/// <param name="Content">Textual content of the turn.</param>
/// <param name="Timestamp">When the turn occurred.</param>
public record ModelTurn(ModelRole Role, string Content, DateTimeOffset Timestamp);
