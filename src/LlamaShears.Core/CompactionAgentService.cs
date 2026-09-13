using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class CompactionAgentService
    : IAgentService,
      IEventHandler<AgentLifecycleMarker>,
      IEventHandler<AgentCompactionRequest>,
      IDisposable
{
    private readonly IDataContextScope _dataScope;
    private readonly IContextStore _contextStore;
    private readonly IAgentPipeline _pipeline;
    private readonly ILogger<CompactionAgentService> _logger;
    private readonly IDisposable _subscriptions;

    public CompactionAgentService(
        IEventBus bus,
        IDataContextScope dataScope,
        IContextStore contextStore,
        IAgentPipeline pipeline,
        ILogger<CompactionAgentService> logger)
    {
        _dataScope = dataScope;
        _contextStore = contextStore;
        _pipeline = pipeline;
        _logger = logger;
        var sessionId = _dataScope.GetCurrentSessionId();
        _subscriptions = DisposableList.Create()
            .And(bus.Subscribe<AgentLifecycleMarker>(
                Event.WellKnown.Agent.Idle with { Id = sessionId },
                EventDeliveryMode.Awaited,
                this,
                preserveSubscriberExecutionContext: true))
            .And(bus.Subscribe<AgentCompactionRequest>(
                Event.WellKnown.Command.CompactionRequest with { Id = sessionId },
                EventDeliveryMode.Awaited,
                this,
                preserveSubscriberExecutionContext: true));
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public ValueTask HandleAsync(IEventEnvelope<AgentLifecycleMarker> envelope, CancellationToken cancellationToken)
        => CompactAsync(force: false, cancellationToken);

    public ValueTask HandleAsync(IEventEnvelope<AgentCompactionRequest> envelope, CancellationToken cancellationToken)
        => CompactAsync(force: envelope.Data?.Force ?? false, cancellationToken);

    public void Dispose() => _subscriptions.Dispose();

    private async ValueTask CompactAsync(bool force, CancellationToken cancellationToken)
    {
        var agentId = _dataScope.GetAgentConfig().Id;
        var live = await _contextStore.OpenAsync(_dataScope.GetCurrentSessionId(), cancellationToken);
        var context = new AgentPipelineContext(live, [], cancellationToken)
        {
            CompactionOnly = true,
            ForceCompaction = force,
        };
        try
        {
            await _pipeline.InvokeAsync(context, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCompactionFailed(agentId, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Compaction failed for agent '{AgentId}'.")]
    private partial void LogCompactionFailed(string agentId, Exception ex);
}
