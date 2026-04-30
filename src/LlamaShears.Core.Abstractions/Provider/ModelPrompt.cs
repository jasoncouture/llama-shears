namespace LlamaShears.Core.Abstractions.Provider;

public record ModelPrompt(IReadOnlyList<ModelTurn> Turns);
