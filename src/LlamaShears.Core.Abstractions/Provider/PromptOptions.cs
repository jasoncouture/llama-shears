namespace LlamaShears.Core.Abstractions.Provider;

public sealed record PromptOptions(
    int? TokenLimit = null
);
