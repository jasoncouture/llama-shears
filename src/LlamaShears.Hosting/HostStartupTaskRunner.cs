using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LlamaShears.Hosting;

/// <summary>
/// <see cref="IHostedService"/> that runs every registered
/// <see cref="IHostStartupTask"/> exactly once when the host starts.
/// All tasks share a single DI scope created in
/// <see cref="StartAsync"/>; the scope is disposed before
/// <see cref="StartAsync"/> returns. Tasks execute in registration
/// order; an exception from any task aborts host startup and prevents
/// later tasks from running.
/// </summary>
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
