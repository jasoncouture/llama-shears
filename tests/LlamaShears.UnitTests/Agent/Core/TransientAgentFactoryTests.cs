using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class TransientAgentFactoryTests
{
    [Test]
    public async Task CreateTransientAgentKeepsTheCallerSessionId()
    {
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice");
        var parent = SessionId.CreateFor(config.Id);
        var child = SessionId.CreateFor(config.Id, "subagent-abc");
        var scope = TestAgentConfigs.DataContextFactoryWith(config, parent).Current!;
        var agentFactory = Substitute.For<IAgentFactory>();
        SessionPath? captured = null;
        agentFactory
            .CreateAgentAsync<ITransientAgent>(
                Arg.Any<AgentConfig>(),
                Arg.Any<SessionPath>(),
                Arg.Any<IEnumerable<KeyValuePair<string, object?>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.Arg<SessionPath>();
                var serviceScope = new AsyncServiceScope(Substitute.For<IServiceScope>());
                return new ValueTask<AgentHandle>(
                    new AgentHandle(
                        captured,
                        "hash",
                        serviceScope,
                        ExecutionContext.Capture()!,
                        typeof(ITransientAgent)));
            });
        ITransientAgentFactory factory = new TransientAgentFactory(agentFactory, scope);
        var prompt = new ModelTurn(ModelRole.User, "go", DateTimeOffset.UnixEpoch);

        var handle = await factory.CreateTransientAgent(
            config,
            child,
            prompt,
            [],
            CancellationToken.None);

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Current).IsEqualTo(child);
        await Assert.That(handle.SessionPath.Current).IsEqualTo(child);
        await Assert.That(handle.SessionPath.Current.Id).IsEqualTo(child.Id);
    }

    [Test]
    public async Task CreateTransientAgentRefusesADefaultSession()
    {
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice");
        var parent = SessionId.CreateFor(config.Id);
        var scope = TestAgentConfigs.DataContextFactoryWith(config, parent).Current!;
        ITransientAgentFactory factory = new TransientAgentFactory(
            Substitute.For<IAgentFactory>(),
            scope);

        await Assert.That(async () => await factory.CreateTransientAgent(
                config,
                SessionId.CreateFor(config.Id),
                new ModelTurn(ModelRole.User, "go", DateTimeOffset.UnixEpoch),
                [],
                CancellationToken.None))
            .Throws<ArgumentException>();
    }
}
