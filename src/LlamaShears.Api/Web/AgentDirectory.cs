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
    private readonly IEventBus _eventPublisher;

    public AgentDirectory(IAgentConfigProvider configProvider, IContextStore contextStore, IEventBus eventPublisher)
    {
        _configProvider = configProvider;
        _contextStore = contextStore;
        _eventPublisher = eventPublisher;
    }

    public IReadOnlyList<string> ListAgentIds() => _configProvider.ListAgentIds();

    public async Task<IReadOnlyList<ModelTurn>> GetTurnsAsync(SessionId session, CancellationToken cancellationToken)
    {
        var context = await _contextStore.OpenAsync(session, cancellationToken);
        return context.Turns;
    }

    public Task ClearAsync(SessionId session, bool archive, CancellationToken cancellationToken)
        => _contextStore.ClearAsync(session, archive, cancellationToken);

    public async Task RequestCompactionAsync(SessionId session, CancellationToken cancellationToken)
    {
        await _eventPublisher.PublishAsync(
            Event.WellKnown.Command.CompactionRequest with { Id = session },
            AgentCompactionRequest.Forced,
            cancellationToken);
    }

    public async Task InterruptAsync(SessionId session, CancellationToken cancellationToken)
    {
        await _eventPublisher.PublishAsync(
            Event.WellKnown.Command.InterruptAgent with { Id = session },
            AgentInterruptRequest.Instance,
            cancellationToken);
    }
}
