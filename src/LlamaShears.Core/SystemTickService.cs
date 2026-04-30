using LlamaShears.Core.Abstractions.Agent;
using MessagePipe;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core;

public class SystemTickService : BackgroundService
{
    private static readonly TimeSpan _tickInterval = TimeSpan.FromSeconds(30);

    private readonly IAsyncPublisher<SystemTick> _publisher;
    private readonly IOptionsMonitor<SystemTickOptions> _options;
    private readonly ILogger<SystemTickService> _logger;

    public SystemTickService(
        IAsyncPublisher<SystemTick> publisher,
        IOptionsMonitor<SystemTickOptions> options,
        ILogger<SystemTickService> logger)
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
                _publisher.Publish(new SystemTick(DateTimeOffset.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish system tick.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
