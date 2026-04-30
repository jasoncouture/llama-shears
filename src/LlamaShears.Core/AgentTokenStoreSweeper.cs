using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core;

internal sealed class AgentTokenStoreSweeper : BackgroundService
{
    private readonly InMemoryAgentTokenStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<AgentTokenStoreOptions> _options;

    public AgentTokenStoreSweeper(
        InMemoryAgentTokenStore store,
        TimeProvider timeProvider,
        IOptions<AgentTokenStoreOptions> options)
    {
        _store = store;
        _timeProvider = timeProvider;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.Value.SweepInterval, _timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                _store.Sweep();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
