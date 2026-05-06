using LlamaShears.Agent.Abstractions;
using MessagePipe;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlamaShears.Agent.Core;

/// <summary>
/// Hosted service that publishes a <see cref="FrameTick"/> on the
/// MessagePipe bus once every 30 seconds (when
/// <see cref="FrameTickOptions.Enabled"/> is true). The frame tick is
/// the lower-level clock signal of the host: it is not itself a
/// heartbeat, but is intended to be consumed by higher-level
/// dispatchers (such as a per-agent heartbeat dispatcher) that decide
/// what work to do on each tick.
/// </summary>
public class FrameTickService : BackgroundService
{
    private static readonly TimeSpan _tickInterval = TimeSpan.FromSeconds(30);

    private readonly IAsyncPublisher<FrameTick> _publisher;
    private readonly IOptionsMonitor<FrameTickOptions> _options;
    private readonly ILogger<FrameTickService> _logger;

    public FrameTickService(
        IAsyncPublisher<FrameTick> publisher,
        IOptionsMonitor<FrameTickOptions> options,
        ILogger<FrameTickService> logger)
    {
        _publisher = publisher;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_tickInterval);

        do
        {
            if (!_options.CurrentValue.Enabled)
            {
                continue;
            }

            try
            {
                _publisher.Publish(new FrameTick(DateTimeOffset.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish frame tick.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
