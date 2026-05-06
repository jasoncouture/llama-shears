using System.Collections.Concurrent;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class EagerCompactor : BackgroundService,
    IEventHandler<AgentMessageFragment>,
    IEventHandler<AgentThoughtFragment>
{
    private static readonly TimeSpan _idleThreshold = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan _scanInterval = TimeSpan.FromMinutes(1);

    private readonly IEventBus _bus;
    private readonly IAgentManager _agents;
    private readonly TimeProvider _time;
    private readonly ILogger<EagerCompactor> _logger;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSeen =
        new(StringComparer.Ordinal);
    private IDisposable? _messageSubscription;
    private IDisposable? _thoughtSubscription;

    public EagerCompactor(
        IEventBus bus,
        IAgentManager agents,
        TimeProvider time,
        ILogger<EagerCompactor> logger)
    {
        _bus = bus;
        _agents = agents;
        _time = time;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _messageSubscription = _bus.Subscribe<AgentMessageFragment>(
            $"{Event.WellKnown.Agent.Message}:+",
            EventDeliveryMode.FireAndForget,
            this);
        _thoughtSubscription = _bus.Subscribe<AgentThoughtFragment>(
            $"{Event.WellKnown.Agent.Thought}:+",
            EventDeliveryMode.FireAndForget,
            this);
        return base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _messageSubscription?.Dispose();
        _thoughtSubscription?.Dispose();
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public ValueTask HandleAsync(IEventEnvelope<AgentMessageFragment> envelope, CancellationToken cancellationToken)
    {
        Touch(envelope.Type.Id);
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(IEventEnvelope<AgentThoughtFragment> envelope, CancellationToken cancellationToken)
    {
        Touch(envelope.Type.Id);
        return ValueTask.CompletedTask;
    }

    private void Touch(string? eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return;
        }
        // Compaction's own fragment events fire under "<agentId>-compaction".
        // Recording those would re-trigger compaction on every scan, which is
        // its own infinite loop.
        if (eventId.EndsWith("-compaction", StringComparison.Ordinal))
        {
            return;
        }
        _lastSeen[eventId] = _time.GetLocalNow();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ScanAsync(stoppingToken).ConfigureAwait(false);
            try
            {
                // Task.Delay (vs PeriodicTimer) so the gap is *between*
                // scans rather than a wall-clock cadence — a scan that
                // touches many agents and runs long can't pile up ticks
                // behind it.
                await Task.Delay(_scanInterval, _time, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        var now = _time.GetLocalNow();
        foreach (var (agentId, lastSeen) in _lastSeen.ToArray())
        {
            if (now - lastSeen < _idleThreshold)
            {
                continue;
            }
            var agent = _agents.Get(agentId);
            if (agent is null)
            {
                _lastSeen.TryRemove(agentId, out _);
                continue;
            }
            // Drop the entry before triggering — a real new fragment after
            // compaction will re-add it. This keeps the dictionary from
            // pinning a stale timestamp that retriggers next scan.
            _lastSeen.TryRemove(agentId, out _);
            try
            {
                await agent.RequestCompactionAsync(cancellationToken).ConfigureAwait(false);
                LogCompactionTriggered(_logger, agentId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogCompactionFailed(_logger, agentId, ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Eager compaction triggered for idle agent '{AgentId}'.")]
    private static partial void LogCompactionTriggered(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Eager compaction failed for agent '{AgentId}'.")]
    private static partial void LogCompactionFailed(ILogger logger, string agentId, Exception ex);
}
