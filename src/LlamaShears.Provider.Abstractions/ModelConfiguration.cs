namespace LlamaShears.Provider.Abstractions;

public record ModelConfiguration(
    string ModelId,
    ThinkLevel Think = ThinkLevel.None,
    int? ContextLength = null,
    IReadOnlyDictionary<string, object>? Parameters = null
);
