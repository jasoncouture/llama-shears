using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Events;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Cron;

public sealed partial class CronExecutor : IEventHandler<SystemTick>, IDisposable
{
    private readonly ICronScheduler _scheduler;
    private readonly TimeProvider _time;
    private readonly ILogger<CronExecutor> _logger;
    private readonly IDisposable _subscription;

    public CronExecutor(
        ICronScheduler scheduler,
        TimeProvider time,
        IEventBus bus,
        ILogger<CronExecutor> logger)
    {
        _scheduler = scheduler;
        _time = time;
        _logger = logger;
        _subscription = bus.Subscribe(
            Event.WellKnown.Host.Tick,
            EventDeliveryMode.FireAndForget,
            this);
    }

    public async ValueTask HandleAsync(IEventEnvelope<SystemTick> envelope, CancellationToken cancellationToken)
    {
        try
        {
            await _scheduler.FireDueAsync(_time.GetUtcNow(), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            LogTickFailed(ex);
        }
    }

    public void Dispose() => _subscription.Dispose();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cron executor tick failed.")]
    private partial void LogTickFailed(Exception ex);
}
