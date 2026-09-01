using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.UnitTests.Agent.Core;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class AgentPipelineContextTests
{
    [Test]
    public async Task TurnTokenDefaultsToShutdownToken()
    {
        using var shutdown = new CancellationTokenSource();
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice"),
            [],
            shutdown.Token);

        await Assert.That(context.TurnToken).IsEqualTo(shutdown.Token);
        await Assert.That(context.ShutdownToken).IsEqualTo(shutdown.Token);
        await Assert.That(context.CorrelationId).IsEqualTo(Guid.Empty);
        await Assert.That(context.Outcome).IsNull();
        await Assert.That(context.SystemPrompt).IsNull();
        await Assert.That(context.EphemeralContext).IsNull();
        await Assert.That(context.Prompt).IsNull();
        await Assert.That(context.SessionId).IsNull();
        await Assert.That(context.Tools.IsDefaultOrEmpty).IsTrue();
    }

    [Test]
    public async Task ConstructorRejectsNullAgentContext()
    {
        await Assert.That(() => new AgentPipelineContext(null!, [], CancellationToken.None))
            .Throws<ArgumentNullException>();
    }
}
