using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Pipeline;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class AgentLifetimeTests
{
    [Test]
    public async Task StopCancelsStopping()
    {
        IAgentLifetime lifetime = new AgentLifetime();

        await Assert.That(lifetime.Stopping.IsCancellationRequested).IsFalse();
        lifetime.Stop();
        await Assert.That(lifetime.Stopping.IsCancellationRequested).IsTrue();
    }

    [Test]
    public async Task StopIsIdempotent()
    {
        IAgentLifetime lifetime = new AgentLifetime();
        lifetime.Stop();
        lifetime.Stop();
        await Assert.That(lifetime.Stopping.IsCancellationRequested).IsTrue();
    }
}
