using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class AgentMiddlewareOrderTests
{
    [Test]
    public async Task BuiltInConstantsAreSpacedOneThousandApart()
    {
        int[] orders =
        [
            AgentMiddlewareOrder.TurnException,
            AgentMiddlewareOrder.AgentActivity,
            AgentMiddlewareOrder.CorrelationScope,
            AgentMiddlewareOrder.AgentLock,
            AgentMiddlewareOrder.InterruptScope,
            AgentMiddlewareOrder.ToolResultEnqueue,
            AgentMiddlewareOrder.RunIteration,
        ];

        await Assert.That(orders).IsEquivalentTo([1000, 2000, 3000, 4000, 5000, 6000, 7000]);
    }

    [Test]
    public async Task BuiltInMiddlewareReportMatchingOrders()
    {
        IAgentMiddleware[] steps =
        [
            new TurnExceptionMiddleware(
                NullLogger<TurnExceptionMiddleware>.Instance,
                PipelineTestContext.ScopeFor()),
            new AgentActivityMiddleware(PipelineTestContext.ScopeFor()),
            new CorrelationScopeMiddleware(NullLogger<CorrelationScopeMiddleware>.Instance),
            new AgentLockMiddleware(Substitute.For<IAgentLock>()),
            new InterruptScopeMiddleware(Substitute.For<IActiveTurnCancellation>()),
            new ToolResultEnqueueMiddleware(Substitute.For<ISessionQueue>()),
            new RunIterationMiddleware(Substitute.For<IAgentIterationRunner>()),
        ];

        int[] expected =
        [
            AgentMiddlewareOrder.TurnException,
            AgentMiddlewareOrder.AgentActivity,
            AgentMiddlewareOrder.CorrelationScope,
            AgentMiddlewareOrder.AgentLock,
            AgentMiddlewareOrder.InterruptScope,
            AgentMiddlewareOrder.ToolResultEnqueue,
            AgentMiddlewareOrder.RunIteration,
        ];

        await Assert.That(steps.Select(step => step.Order).ToArray()).IsEquivalentTo(expected);
    }
}
