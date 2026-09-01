using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.UnitTests.Agent.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentConfigReloadServiceTests
{
    [Test]
    public async Task HandleAsyncReplacesTheScopedConfig()
    {
        var scope = PipelineTestContext.ScopeFor("alice");
        var updated = TestAgentConfigs.WithHeartbeat(TimeSpan.FromMinutes(1), "alice");
        IAgentService service = new AgentConfigReloadService(
            Substitute.For<IEventBus>(),
            scope,
            NullLogger<AgentConfigReloadService>.Instance);
        var envelope = Substitute.For<IEventEnvelope<ConfigurationChangedNotification>>();
        envelope.Data.Returns(new ConfigurationChangedNotification(
            TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice"),
            updated));

        await ((IEventHandler<ConfigurationChangedNotification>)service).HandleAsync(
            envelope,
            CancellationToken.None);

        await Assert.That(scope.GetAgentConfig()).IsEqualTo(updated);
    }

    [Test]
    public async Task HandleAsyncIgnoresATombstone()
    {
        var scope = PipelineTestContext.ScopeFor("alice");
        var original = scope.GetAgentConfig();
        IAgentService service = new AgentConfigReloadService(
            Substitute.For<IEventBus>(),
            scope,
            NullLogger<AgentConfigReloadService>.Instance);
        var envelope = Substitute.For<IEventEnvelope<ConfigurationChangedNotification>>();
        envelope.Data.Returns(new ConfigurationChangedNotification(original, UpdatedConfig: null));

        await ((IEventHandler<ConfigurationChangedNotification>)service).HandleAsync(
            envelope,
            CancellationToken.None);

        await Assert.That(scope.GetAgentConfig()).IsEqualTo(original);
    }
}
