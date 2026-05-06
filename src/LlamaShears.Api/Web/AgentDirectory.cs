using LlamaShears.Api.Web.Services;
using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Api.Web;

internal sealed class AgentDirectory : IAgentDirectory
{
    private readonly AgentManager _manager;
    private readonly IContextStore _contextStore;

    public AgentDirectory(AgentManager manager, IContextStore contextStore)
    {
        _manager = manager;
        _contextStore = contextStore;
    }

    public IReadOnlyList<string> ListAgentIds()
        => [.. _manager.Agents.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)];

    public async Task<IReadOnlyList<ModelTurn>> GetTurnsAsync(string agentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        var context = await _contextStore.OpenAsync(agentId, cancellationToken).ConfigureAwait(false);
        return context.Turns;
    }

    public Task ClearAsync(string agentId, bool archive, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        return _contextStore.ClearAsync(agentId, archive, cancellationToken);
    }
}
