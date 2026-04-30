namespace LlamaShears.Core.Abstractions.Provider;

public record ModelTurn(ModelRole Role, string Content, DateTimeOffset Timestamp) : IContextEntry;
