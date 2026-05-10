using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Agent;

public static class AgentConfigExtensions
{
    public static AgentConfig? GetAgentConfig(this IDataContextScope scope)
    {
        scope.TryGetValue<AgentConfig>(AgentConfig.DataKey, out var config);
        return config;
    }
}
