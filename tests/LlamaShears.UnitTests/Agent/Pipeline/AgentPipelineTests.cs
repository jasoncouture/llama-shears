using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.UnitTests.Agent.Core;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class AgentPipelineTests
{
    [Test]
    public async Task InvokeAsyncRunsMiddlewareInRegistrationOrderOuterToInner()
    {
        var trace = new List<string>();
        IAgentPipeline pipeline = new AgentPipeline(
        [
            new RecordingAgentMiddleware("outer", trace),
            new RecordingAgentMiddleware("inner", trace),
        ]);

        await pipeline.InvokeAsync(CreateContext(), CancellationToken.None);

        await Assert.That(trace).IsEquivalentTo(
        [
            "outer-before",
            "inner-before",
            "inner-after",
            "outer-after",
        ]);
    }

    [Test]
    public async Task InvokeAsyncShortCircuitSkipsRemainingMiddleware()
    {
        var trace = new List<string>();
        IAgentPipeline pipeline = new AgentPipeline(
        [
            new RecordingAgentMiddleware("gate", trace, callNext: false),
            new RecordingAgentMiddleware("inner", trace),
        ]);

        await pipeline.InvokeAsync(CreateContext(), CancellationToken.None);

        await Assert.That(trace).IsEquivalentTo(["gate-before", "gate-after"]);
    }

    [Test]
    public async Task InvokeAsyncWithNoMiddlewareCompletes()
    {
        IAgentPipeline pipeline = new AgentPipeline([]);

        await pipeline.InvokeAsync(CreateContext(), CancellationToken.None);
    }

    [Test]
    public async Task InvokeAsyncRejectsNullContext()
    {
        IAgentPipeline pipeline = new AgentPipeline([]);

        await Assert.That(() => pipeline.InvokeAsync(null!, CancellationToken.None))
            .Throws<ArgumentNullException>();
    }

    private static AgentPipelineContext CreateContext()
        => new(
            new FakeAgentContext("alice"),
            [],
            CancellationToken.None);
}
