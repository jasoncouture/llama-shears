using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Pipeline;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class ToolLoopLimitMiddlewareTests
{
    [Test]
    public async Task ReportsToolLoopLimitOrder()
    {
        IAgentMiddleware middleware = new ToolLoopLimitMiddleware(Substitute.For<IToolLoopBudget>());

        await Assert.That(middleware.Order).IsEqualTo(AgentMiddlewareOrder.ToolLoopLimit);
    }

    [Test]
    public async Task DropsLeftoverToolCallsWhenBudgetIsFinal()
    {
        var budget = Substitute.For<IToolLoopBudget>();
        budget.IsFinal.Returns(true);
        IAgentMiddleware middleware = new ToolLoopLimitMiddleware(budget);
        var context = PipelineTestContext.Create();
        context.Tools = [new ToolGroup("llamashears", [])];
        context.Outcome = new IterationOutcome(
            Interrupted: false,
            ToolResultTurns: [],
            ToolCalls: [new ToolCall("llamashears", "file_read", "{}", "1")]);
        var nextRan = false;

        await middleware.InvokeAsync(context, (_, _) =>
        {
            nextRan = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        await Assert.That(nextRan).IsTrue();
        await Assert.That(context.Outcome!.ToolCalls.IsDefaultOrEmpty).IsTrue();
        await Assert.That(context.Tools.IsDefaultOrEmpty).IsTrue();
    }

    [Test]
    public async Task LeavesToolCallsWhenBudgetAllows()
    {
        var budget = Substitute.For<IToolLoopBudget>();
        budget.IsFinal.Returns(false);
        IAgentMiddleware middleware = new ToolLoopLimitMiddleware(budget);
        var call = new ToolCall("llamashears", "file_read", "{}", "1");
        var context = PipelineTestContext.Create();
        context.Outcome = new IterationOutcome(
            Interrupted: false,
            ToolResultTurns: [],
            ToolCalls: [call]);

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await Assert.That(context.Outcome!.ToolCalls.Length).IsEqualTo(1);
        await Assert.That(context.Outcome.ToolCalls[0]).IsEqualTo(call);
    }
}
