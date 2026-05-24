using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.PromptContext;

public sealed class AgentBehaviorSkillFilter : ISkillFilter
{
    private readonly IDataContextFactory _scopes;

    public AgentBehaviorSkillFilter(IDataContextFactory scopes)
    {
        _scopes = scopes;
    }

    public bool AreSkillsAllowed()
        => AgentBehaviorFilterPolicy.IsEnabled(GetOptions()?.Skills);

    public bool IsSkillAllowed(string skillName)
        => AgentBehaviorFilterPolicy.Allowed(GetOptions()?.Skills, skillName);

    private AgentBehaviorOptions? GetOptions()
        => _scopes.Current?.TryGetAgentConfig()?.Security;
}
