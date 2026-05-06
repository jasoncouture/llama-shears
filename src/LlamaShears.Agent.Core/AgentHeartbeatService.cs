using LlamaShears.Agent.Abstractions;
using MessagePipe;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlamaShears.Agent.Core;

/// <summary>
/// Hosted service that publishes a <see cref="HeartbeatTick"/> on the
/// MessagePipe bus once per minute (when
/// <see cref="AgentHeartbeatOptions.Enabled"/> is true). Agents and
/// other components subscribe to <see cref="HeartbeatTick"/> to decide
/// whether to act on each tick.
/// </summary>
public class AgentHeartbeatService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);

    private readonly IAsyncPublisher<HeartbeatTick> _publisher;
    private readonly IOptionsMonitor<AgentHeartbeatOptions> _options;
    private readonly ILogger<AgentHeartbeatService> _logger;

    public AgentHeartbeatService(
        IAsyncPublisher<HeartbeatTick> publisher,
        IOptionsMonitor<AgentHeartbeatOptions> options,
        ILogger<AgentHeartbeatService> logger)
    {
        _publisher = publisher;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);

        do
        {
            if (!_options.CurrentValue.Enabled)
            {
                continue;
            }

            try
            {
                _publisher.Publish(new HeartbeatTick(DateTimeOffset.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish heartbeat tick.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
