using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Pipeline;
using LlamaShears.Core.Tools.ModelContextProtocol;
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
            AgentMiddlewareOrder.SystemPrompt,
            AgentMiddlewareOrder.Compaction,
            AgentMiddlewareOrder.EphemeralContext,
            AgentMiddlewareOrder.RunIteration,
            AgentMiddlewareOrder.ToolDispatch,
            AgentMiddlewareOrder.StripImageAttachments,
        ];

        await Assert.That(orders).IsEquivalentTo([1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000, 11000, 12000]);
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
            new SystemPromptMiddleware(
                Substitute.For<ISystemPromptProvider>(),
                PipelineTestContext.ScopeFor(),
                TimeProvider.System),
            new CompactionMiddleware(
                Substitute.For<IContextCompactor>(),
                Substitute.For<IAgentContextProvider>(),
                Substitute.For<IEventBus>(),
                PipelineTestContext.ScopeFor()),
            new EphemeralContextMiddleware(
                Substitute.For<IPromptContextProvider>(),
                Substitute.For<IMemorySearcher>(),
                Substitute.For<IAgentStateTracker>(),
                PipelineTestContext.ScopeFor(),
                TimeProvider.System),
            new RunIterationMiddleware(
                Substitute.For<IAgentIterationRunner>(),
                PipelineTestContext.ScopeFor()),
            new ToolDispatchMiddleware(
                new ToolCallExecutor(
                    Substitute.For<IEventBus>(),
                    Substitute.For<IToolCallDispatcher>(),
                    TimeProvider.System,
                    NullLogger<ToolCallExecutor>.Instance)),
            new StripImageAttachmentsMiddleware(),
        ];

        int[] expected =
        [
            AgentMiddlewareOrder.TurnException,
            AgentMiddlewareOrder.AgentActivity,
            AgentMiddlewareOrder.CorrelationScope,
            AgentMiddlewareOrder.AgentLock,
            AgentMiddlewareOrder.InterruptScope,
            AgentMiddlewareOrder.ToolResultEnqueue,
            AgentMiddlewareOrder.SystemPrompt,
            AgentMiddlewareOrder.Compaction,
            AgentMiddlewareOrder.EphemeralContext,
            AgentMiddlewareOrder.RunIteration,
            AgentMiddlewareOrder.ToolDispatch,
            AgentMiddlewareOrder.StripImageAttachments,
        ];

        await Assert.That(steps.Select(step => step.Order).ToArray()).IsEquivalentTo(expected);
    }
}
