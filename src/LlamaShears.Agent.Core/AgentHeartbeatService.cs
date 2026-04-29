using LlamaShears.Agent.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlamaShears.Agent.Core;

/// <summary>
/// Hosted service that ticks every minute and invokes
/// <see cref="IAgent.HeartbeatAsync"/> on every agent whose
/// <see cref="IAgent.HeartbeatPeriod"/> has elapsed since its
/// <see cref="IAgent.LastHeartbeatAt"/>.
/// </summary>
public class AgentHeartbeatService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);

    private readonly IOptionsMonitor<AgentHeartbeatOptions> _options;
    private readonly IEnumerable<IAgent> _agents;
    private readonly ILogger<AgentHeartbeatService> _logger;

    public AgentHeartbeatService(
        IOptionsMonitor<AgentHeartbeatOptions> options,
        IEnumerable<IAgent> agents,
        ILogger<AgentHeartbeatService> logger)
    {
        _options = options;
        _agents = agents;
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

            var now = DateTimeOffset.UtcNow;
            foreach (var agent in _agents)
            {
                if (!agent.HeartbeatEnabled)
                {
                    continue;
                }

                if (now - agent.LastHeartbeatAt < agent.HeartbeatPeriod)
                {
                    continue;
                }

                try
                {
                    await agent.HeartbeatAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Heartbeat failed for agent {Agent}.", agent);
                }
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
