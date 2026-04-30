using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Context;

namespace LlamaShears.Core.Context;

public sealed class AgentContextProvider : IAgentContextProvider
{
    private readonly IAgentConfigProvider _configProvider;
    private readonly IContextStore _contextStore;
    private readonly TimeProvider _timeProvider;

    public AgentContextProvider(
        IAgentConfigProvider configProvider,
        IContextStore contextStore,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(configProvider);
        ArgumentNullException.ThrowIfNull(contextStore);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _configProvider = configProvider;
        _contextStore = contextStore;
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

        var persisted = _contextStore.OpenAsync(agentId, CancellationToken.None)
            .GetAwaiter().GetResult();

        return new AgentContext(
            AgentId: agentId,
            Now: _timeProvider.GetUtcNow(),
            Config: config,
            LanguageModel: new LanguageModelContext(
                Turns: [.. persisted.Turns],
                Entries: [.. persisted.Entries]),
            System: new SystemContext(),
            Tools: new ToolContext([]),
            Plugins: new PluginContext([]));
    }
}
