using System.Diagnostics;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Pipeline;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class AgentActivityMiddlewareTests
{
    [Test]
    public async Task StartsAChatActivityWithGenAiTags()
    {
        IAgentMiddleware middleware = new AgentActivityMiddleware(PipelineTestContext.ScopeFor());
        Activity? started = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.Contains("AgentActivityMiddleware", StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => started = activity,
        };
        ActivitySource.AddActivityListener(listener);

        await middleware.InvokeAsync(
            PipelineTestContext.Create(),
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        await Assert.That(started).IsNotNull();
        await Assert.That(started!.DisplayName).StartsWith("chat ");
        await Assert.That(started.GetTagItem("gen_ai.system")).IsEqualTo("llamashears");
        await Assert.That(started.GetTagItem("gen_ai.agent.id")).IsEqualTo("alice");
        await Assert.That(started.GetTagItem("gen_ai.operation.name")).IsEqualTo("chat");
    }

    [Test]
    public async Task FailedNextMarksTheActivityAndRethrows()
    {
        IAgentMiddleware middleware = new AgentActivityMiddleware(PipelineTestContext.ScopeFor());
        Activity? started = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.Contains("AgentActivityMiddleware", StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => started = activity,
        };
        ActivitySource.AddActivityListener(listener);

        await Assert.That(() => middleware.InvokeAsync(
                PipelineTestContext.Create(),
                (_, _) => throw new InvalidOperationException("boom"),
                CancellationToken.None))
            .Throws<InvalidOperationException>();

        await Assert.That(started).IsNotNull();
        await Assert.That(started!.Status).IsEqualTo(ActivityStatusCode.Error);
    }
}
