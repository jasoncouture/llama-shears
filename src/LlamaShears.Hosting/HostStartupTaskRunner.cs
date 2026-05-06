using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LlamaShears.Hosting;

public sealed class HostStartupTaskRunner : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public HostStartupTaskRunner(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var tasks = scope.ServiceProvider.GetServices<IHostStartupTask>();

        foreach (var task in tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await task.StartAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
