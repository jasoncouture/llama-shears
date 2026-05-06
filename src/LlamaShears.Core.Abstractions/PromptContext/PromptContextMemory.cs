namespace LlamaShears.Core.Abstractions.PromptContext;

public sealed record PromptContextMemory(string RelativePath, string Content, double Score);
