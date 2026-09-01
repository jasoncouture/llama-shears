using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Pipeline;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class RunIterationMiddlewareTests
{
    [Test]
    public async Task StoresTheIterationOutcomeOnTheContext()
    {
        var runner = Substitute.For<IAgentIterationRunner>();
        var expected = new IterationOutcome(Interrupted: false, ToolResultTurns: []);
        runner.RunAsync(Arg.Any<AgentPipelineContext>()).Returns(expected);
        IAgentMiddleware middleware = new RunIterationMiddleware(runner);
        var context = PipelineTestContext.Create();
        context.CorrelationId = Guid.CreateVersion7();
        var nextCalled = false;

        await middleware.InvokeAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(context.Outcome).IsEqualTo(expected);
        await Assert.That(nextCalled).IsTrue();
        await runner.Received(1).RunAsync(context);
    }
}
