using LlamaShears.Api.Web.Services;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Api.Web;

internal sealed class AgentDirectory : IAgentDirectory
{
    private readonly IAgentConfigProvider _configProvider;
    private readonly IContextStore _contextStore;

    public AgentDirectory(IAgentConfigProvider configProvider, IContextStore contextStore)
    {
        _configProvider = configProvider;
        _contextStore = contextStore;
    }

    public IReadOnlyList<string> ListAgentIds() => _configProvider.ListAgentIds();

    public async Task<IReadOnlyList<ModelTurn>> GetTurnsAsync(SessionId session, CancellationToken cancellationToken)
    {
        var context = await _contextStore.OpenAsync(session, cancellationToken);
        return context.Turns;
    }

    public Task ClearAsync(SessionId session, bool archive, CancellationToken cancellationToken)
        => _contextStore.ClearAsync(session, archive, cancellationToken);
}
