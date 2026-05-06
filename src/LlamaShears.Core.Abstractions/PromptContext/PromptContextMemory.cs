namespace LlamaShears.Core.Abstractions.PromptContext;

public sealed record PromptContextMemory(string RelativePath, string Summary, double Score);
