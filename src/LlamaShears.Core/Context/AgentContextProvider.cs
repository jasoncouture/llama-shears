using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Context;

namespace LlamaShears.Core.Context;

public sealed class AgentContextProvider : IAgentContextProvider
{
    private readonly IAgentConfigProvider _configProvider;
    private readonly TimeProvider _timeProvider;

    public AgentContextProvider(IAgentConfigProvider configProvider, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(configProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _configProvider = configProvider;
        _timeProvider = timeProvider;
    }

    public AgentContext? CreateAgentContext()
        => throw new NotImplementedException();

    public AgentContext? CreateAgentContext(string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        var config = _configProvider.GetConfig(agentId);
        if (config is null)
        {
            return null;
        }

        return new AgentContext(
            AgentId: agentId,
            Now: _timeProvider.GetUtcNow(),
            Config: config,
            LanguageModel: new LanguageModelContext([], []),
            System: new SystemContext(),
            Tools: new ToolContext([]),
            Plugins: new PluginContext([]));
    }
}
