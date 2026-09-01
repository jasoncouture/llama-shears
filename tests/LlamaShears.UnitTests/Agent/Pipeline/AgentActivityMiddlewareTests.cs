using System.Diagnostics;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Pipeline;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class AgentActivityMiddlewareTests
{
    [Test]
    public async Task StartsAChatActivityWithGenAiTags()
    {
        var agentId = $"alice-tags-{Guid.CreateVersion7():N}";
        IAgentMiddleware middleware = new AgentActivityMiddleware(PipelineTestContext.ScopeFor(agentId));
        Activity? started = null;
        using var listener = ListenForAgent(agentId, activity => started = activity);

        await middleware.InvokeAsync(
            PipelineTestContext.Create(),
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        await Assert.That(started).IsNotNull();
        await Assert.That(started!.DisplayName).StartsWith("chat ");
        await Assert.That(started.GetTagItem("gen_ai.system")).IsEqualTo("llamashears");
        await Assert.That(started.GetTagItem("gen_ai.agent.id")).IsEqualTo(agentId);
        await Assert.That(started.GetTagItem("gen_ai.operation.name")).IsEqualTo("chat");
    }

    [Test]
    public async Task FailedNextMarksTheActivityAndRethrows()
    {
        var agentId = $"alice-fail-{Guid.CreateVersion7():N}";
        IAgentMiddleware middleware = new AgentActivityMiddleware(PipelineTestContext.ScopeFor(agentId));
        Activity? started = null;
        using var listener = ListenForAgent(agentId, activity => started = activity);

        await Assert.That(() => middleware.InvokeAsync(
                PipelineTestContext.Create(),
                (_, _) => throw new InvalidOperationException("boom"),
                CancellationToken.None))
            .Throws<InvalidOperationException>();

        await Assert.That(started).IsNotNull();
        await Assert.That(started!.Status).IsEqualTo(ActivityStatusCode.Error);
    }

    private static ActivityListener ListenForAgent(string agentId, Action<Activity> onStopped)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.Contains("AgentActivityMiddleware", StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (Equals(activity.GetTagItem("gen_ai.agent.id"), agentId))
                {
                    onStopped.Invoke(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
