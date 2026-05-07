using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core.Cron;

public sealed partial class CronExecutor : BackgroundService
{
    private readonly ICronScheduler _scheduler;
    private readonly TimeProvider _time;
    private readonly IOptionsMonitor<CronOptions> _options;
    private readonly ILogger<CronExecutor> _logger;

    public CronExecutor(
        ICronScheduler scheduler,
        TimeProvider time,
        IOptionsMonitor<CronOptions> options,
        ILogger<CronExecutor> logger)
    {
        _scheduler = scheduler;
        _time = time;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(_logger, _options.CurrentValue.TickInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _scheduler.FireDueAsync(_time.GetUtcNow(), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                LogTickFailed(_logger, ex);
            }

            try
            {
                await Task.Delay(_options.CurrentValue.TickInterval, _time, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Cron executor started; tick interval {TickInterval}.")]
    private static partial void LogStarted(ILogger logger, TimeSpan tickInterval);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cron executor tick failed.")]
    private static partial void LogTickFailed(ILogger logger, Exception ex);
}
