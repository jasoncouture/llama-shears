namespace LlamaShears.Provider.Abstractions;

public record ModelConfiguration(
    string ModelId,
    ThinkLevel Think = ThinkLevel.None,
    IReadOnlyDictionary<string, object>? Parameters = null
);
