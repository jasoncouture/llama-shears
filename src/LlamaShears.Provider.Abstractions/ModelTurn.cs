namespace LlamaShears.Provider.Abstractions;

public record ModelTurn(ModelRole Role, string Content, DateTimeOffset Timestamp);
