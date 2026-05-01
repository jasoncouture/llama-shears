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

    public ValueTask<AgentContext?> CreateAgentContextAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public async ValueTask<AgentContext?> CreateAgentContextAsync(string agentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        var config = await _configProvider.GetConfigAsync(agentId, cancellationToken).ConfigureAwait(false);
        if (config is null)
        {
            return null;
        }

        var persisted = await _contextStore.OpenAsync(agentId, cancellationToken).ConfigureAwait(false);

        return new AgentContext(
            AgentId: agentId,
            Now: _timeProvider.GetUtcNow(),
            Config: config,
            LanguageModel: new LanguageModelContext(
                Turns: [.. persisted.Turns],
                Entries: [.. persisted.Entries],
                persisted.TokenCount),
            System: new SystemContext(),
            Tools: new ToolContext([]),
            Plugins: new PluginContext([]));
    }
}
