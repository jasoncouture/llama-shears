using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.PromptContext;

public sealed class AgentBehaviorToolFilter : IToolFilter
{
    private readonly IDataContextFactory _scopes;

    public AgentBehaviorToolFilter(IDataContextFactory scopes)
    {
        _scopes = scopes;
    }

    public bool AreToolsAllowed()
        => AgentBehaviorFilterPolicy.IsEnabled(GetOptions()?.Tools);

    public bool IsSourceAllowed(string source)
        => AgentBehaviorFilterPolicy.Allowed(GetOptions()?.Sources, source);

    public bool IsToolAllowed(string source, string toolName)
        => AgentBehaviorFilterPolicy.Allowed(GetOptions()?.Tools, toolName);

    private AgentBehaviorOptions? GetOptions()
        => _scopes.Current?.TryGetAgentConfig()?.Security;
}
