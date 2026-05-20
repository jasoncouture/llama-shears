using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
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

    public async ValueTask<AgentContext?> CreateAgentContextAsync(SessionId session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var config = await _configProvider.GetConfigAsync(session.AgentId, cancellationToken);
        if (config is null)
        {
            return null;
        }

        var persisted = await _contextStore.OpenAsync(session, cancellationToken);

        return new AgentContext(
            AgentId: session.AgentId,
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
