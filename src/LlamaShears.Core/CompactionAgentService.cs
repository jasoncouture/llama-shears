using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class CompactionAgentService
    : IEventHandler<AgentLifecycleMarker>,
      IEventHandler<AgentCompactionRequest>,
      IDisposable
{
    private readonly IDataContextScope _dataScope;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IContextStore _contextStore;
    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IAgentLock _agentLock;
    private readonly ILogger<CompactionAgentService> _logger;
    private readonly IDisposable _idleSubscription;
    private readonly IDisposable _requestedSubscription;

    public CompactionAgentService(
        IEventBus bus,
        IDataContextScope dataScope,
        IServiceScopeFactory scopeFactory,
        IContextStore contextStore,
        IAgentContextProvider agentContextProvider,
        IAgentLock agentLock,
        ILogger<CompactionAgentService> logger)
    {
        _dataScope = dataScope;
        _scopeFactory = scopeFactory;
        _contextStore = contextStore;
        _agentContextProvider = agentContextProvider;
        _agentLock = agentLock;
        _logger = logger;
        var agentId = _dataScope.GetAgentConfig().Id;
        _idleSubscription = bus.Subscribe<AgentLifecycleMarker>(
            Event.WellKnown.Agent.Idle with { Id = agentId },
            EventDeliveryMode.Awaited,
            this);
        _requestedSubscription = bus.Subscribe<AgentCompactionRequest>(
            Event.WellKnown.Agent.CompactionRequested with { Id = agentId },
            EventDeliveryMode.Awaited,
            this);
    }

    public ValueTask HandleAsync(IEventEnvelope<AgentLifecycleMarker> envelope, CancellationToken cancellationToken)
        => CompactAsync(force: false, cancellationToken);

    public ValueTask HandleAsync(IEventEnvelope<AgentCompactionRequest> envelope, CancellationToken cancellationToken)
        => CompactAsync(force: envelope.Data?.Force ?? false, cancellationToken);

    public void Dispose()
    {
        _idleSubscription.Dispose();
        _requestedSubscription.Dispose();
    }

    private async ValueTask CompactAsync(bool force, CancellationToken cancellationToken)
    {
        var agentId = _dataScope.GetAgentConfig().Id;
        using var lockScope = await _agentLock.AcquireLockAsync(cancellationToken);
        await using var bundle = _scopeFactory.CreateAsyncScopeWithData();
        await bundle.ServiceScope.ApplyScopeDataAsync(cancellationToken);

        var agentContext = await _contextStore.OpenAsync(agentId, cancellationToken);
        var prompt = new ModelPrompt([.. agentContext.Turns]);
        var snapshot = await _agentContextProvider.CreateAgentContextAsync(agentId, cancellationToken)
                           .ConfigureAwait(false)
                       ?? throw new InvalidOperationException(
                           $"Agent context provider returned null for running agent '{agentId}'.");
        var compactor = bundle.ServiceProvider.GetRequiredService<IContextCompactor>();
        try
        {
            await compactor.CompactAsync(snapshot, prompt, force: force, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCompactionFailed(agentId, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Compaction failed for agent '{AgentId}'.")]
    private partial void LogCompactionFailed(string agentId, Exception ex);
}
