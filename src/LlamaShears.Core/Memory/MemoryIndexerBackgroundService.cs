using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core.Memory;

public sealed partial class MemoryIndexerBackgroundService : BackgroundService
{
    private readonly IAgentConfigProvider _configs;
    private readonly IMemoryIndexer _indexer;
    private readonly TimeProvider _time;
    private readonly IOptionsMonitor<MemoryServiceOptions> _options;
    private readonly ILogger<MemoryIndexerBackgroundService> _logger;

    public MemoryIndexerBackgroundService(
        IAgentConfigProvider configs,
        IMemoryIndexer indexer,
        TimeProvider time,
        IOptionsMonitor<MemoryServiceOptions> options,
        ILogger<MemoryIndexerBackgroundService> logger)
    {
        _configs = configs;
        _indexer = indexer;
        _time = time;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startupOptions = _options.CurrentValue.Indexer;
        if (!startupOptions.Enabled)
        {
            LogDisabled(_logger);
            return;
        }
        if (startupOptions.Interval <= TimeSpan.Zero)
        {
            LogInvalidInterval(_logger, startupOptions.Interval);
            return;
        }

        var first = true;
        while (!stoppingToken.IsCancellationRequested)
        {
            var indexerOptions = _options.CurrentValue.Indexer;
            if (!indexerOptions.Enabled)
            {
                return;
            }
            var force = first && indexerOptions.ForceOnStartup;
            first = false;
            await ScanAsync(force, stoppingToken).ConfigureAwait(false);

            try
            {
                // Task.Delay over PeriodicTimer so a long scan doesn't
                // queue ticks behind it — the gap is *between* scans.
                await Task.Delay(indexerOptions.Interval, _time, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task ScanAsync(bool force, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> agentIds;
        try
        {
            agentIds = _configs.ListAgentIds();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogListFailed(_logger, ex);
            return;
        }

        LogScanStarting(_logger, agentIds.Count, force);
        foreach (var agentId in agentIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                LogReconcilingAgent(_logger, agentId, force);
                var summary = await _indexer.ReconcileAsync(agentId, force, cancellationToken).ConfigureAwait(false);
                LogReconciled(_logger, agentId, summary.Added, summary.Updated, summary.Removed, summary.Total);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogReconcileFailed(_logger, agentId, ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Memory indexer disabled by configuration.")]
    private static partial void LogDisabled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Memory indexer interval '{Interval}' is non-positive; service will not run.")]
    private static partial void LogInvalidInterval(ILogger logger, TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Memory indexer failed to enumerate agents.")]
    private static partial void LogListFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Memory indexer scan starting: {AgentCount} agent(s), force={Force}.")]
    private static partial void LogScanStarting(ILogger logger, int agentCount, bool force);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reconciling memory index for agent '{AgentId}' (force={Force})…")]
    private static partial void LogReconcilingAgent(ILogger logger, string agentId, bool force);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reconciled agent '{AgentId}': {Added} added, {Updated} updated, {Removed} removed, {Total} total.")]
    private static partial void LogReconciled(ILogger logger, string agentId, int added, int updated, int removed, int total);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Memory indexer failed to reconcile agent '{AgentId}'.")]
    private static partial void LogReconcileFailed(ILogger logger, string agentId, Exception ex);
}
