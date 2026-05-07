using LlamaShears.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LlamaShears.UnitTests.Hosting;

public sealed class HostRestarterTests
{
    [Test]
    public async Task RequestRestartCallsStopApplicationOnce()
    {
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopped.Returns(CancellationToken.None);

        var restarter = new HostRestarter(lifetime, NullLogger<HostRestarter>.Instance);

        restarter.RequestRestart();

        lifetime.Received(1).StopApplication();
        await Task.CompletedTask;
    }

    [Test]
    public async Task RequestRestartIsIdempotent()
    {
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopped.Returns(CancellationToken.None);

        var restarter = new HostRestarter(lifetime, NullLogger<HostRestarter>.Instance);

        restarter.RequestRestart();
        restarter.RequestRestart();
        restarter.RequestRestart();

        lifetime.Received(1).StopApplication();
        await Task.CompletedTask;
    }
}
