using LlamaShears.Api.Web.Services;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Api.Web;

internal sealed class AgentDirectory : IAgentDirectory
{
    private readonly IAgentManager _manager;
    private readonly IContextStore _contextStore;
    private readonly IEventPublisher _eventPublisher;

    public AgentDirectory(IAgentManager manager, IContextStore contextStore, IEventPublisher eventPublisher)
    {
        _manager = manager;
        _contextStore = contextStore;
        _eventPublisher = eventPublisher;
    }

    public IReadOnlyList<string> ListAgentIds() => _manager.AgentIds;

    public async Task<IReadOnlyList<ModelTurn>> GetTurnsAsync(string agentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        var context = await _contextStore.OpenAsync(agentId, cancellationToken);
        return context.Turns;
    }

    public Task ClearAsync(string agentId, bool archive, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        return _contextStore.ClearAsync(agentId, archive, cancellationToken);
    }

    public async Task RequestCompactionAsync(string agentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        if (_manager.Get(agentId) is null)
        {
            throw new InvalidOperationException($"Agent '{agentId}' is not loaded.");
        }
        await _eventPublisher.PublishAsync(
            Event.WellKnown.Command.CompactionRequest with { Id = agentId },
            AgentCompactionRequest.Forced,
            cancellationToken);
    }

    public async Task InterruptAsync(string agentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        if (_manager.Get(agentId) is null)
        {
            throw new InvalidOperationException($"Agent '{agentId}' is not loaded.");
        }
        await _eventPublisher.PublishAsync(
            Event.WellKnown.Command.InterruptAgent with { Id = agentId },
            AgentInterruptRequest.Instance,
            cancellationToken);
    }
}
