using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.PromptContext;

public sealed class NoOpToolFilter : IToolFilter
{
    public bool AreToolsAllowed() => true;

    public bool IsSourceAllowed(string source) => true;

    public bool IsToolAllowed(string source, string toolName) => true;
}
