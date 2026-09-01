using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Pipeline;
using LlamaShears.Core.Tools.ModelContextProtocol;
using LlamaShears.UnitTests.Agent.Core;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class ToolDispatchMiddlewareTests
{
    [Test]
    public async Task DispatchesOutcomeToolCallsAndWritesResultTurns()
    {
        var dispatcher = Substitute.For<IToolCallDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<ToolCall>(), Arg.Any<System.Collections.Immutable.ImmutableArray<ToolGroup>>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => new ValueTask<ToolCallResult>(
                new ToolCallResult($"result-{call.Arg<ToolCall>().CallId}", IsError: false)));
        var session = new SessionId("alice", SessionId.DefaultSessionName);
        IAgentMiddleware middleware = new ToolDispatchMiddleware(
            new ToolCallExecutor(
                Substitute.For<IEventBus>(),
                dispatcher,
                TimeProvider.System,
                NullLogger<ToolCallExecutor>.Instance));
        var callA = new ToolCall("llamashears", "file_read", "{}", "1");
        var callB = new ToolCall("llamashears", "file_read", "{}", "2");
        var context = PipelineTestContext.Create();
        context.SessionId = session;
        context.CorrelationId = Guid.CreateVersion7();
        context.Tools = [new ToolGroup("llamashears", [])];
        context.Outcome = new IterationOutcome(
            Interrupted: false,
            ToolResultTurns: [],
            ToolCalls: [callA, callB]);

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await Assert.That(context.Outcome!.ToolResultTurns.Length).IsEqualTo(2);
        await Assert.That(context.Outcome.ToolResultTurns[0].Content).IsEqualTo("result-1");
        await Assert.That(context.Outcome.ToolResultTurns[1].Content).IsEqualTo("result-2");
        await dispatcher.Received(2).DispatchAsync(
            Arg.Any<ToolCall>(),
            context.Tools,
            session,
            context.CorrelationId,
            context.TurnToken);
    }

    [Test]
    public async Task SkipsDispatchWhenThereAreNoToolCalls()
    {
        var dispatcher = Substitute.For<IToolCallDispatcher>();
        IAgentMiddleware middleware = new ToolDispatchMiddleware(
            new ToolCallExecutor(
                Substitute.For<IEventBus>(),
                dispatcher,
                TimeProvider.System,
                NullLogger<ToolCallExecutor>.Instance));
        var context = PipelineTestContext.Create();
        context.SessionId = new SessionId("alice", SessionId.DefaultSessionName);
        context.Outcome = new IterationOutcome(Interrupted: false, ToolResultTurns: []);

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<ToolCall>(),
            Arg.Any<System.Collections.Immutable.ImmutableArray<ToolGroup>>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchesOnInterruptSoCallsPairWithResults()
    {
        var dispatcher = Substitute.For<IToolCallDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<ToolCall>(), Arg.Any<System.Collections.Immutable.ImmutableArray<ToolGroup>>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ToolCallResult>(new ToolCallResult("interrupted", IsError: true)));
        IAgentMiddleware middleware = new ToolDispatchMiddleware(
            new ToolCallExecutor(
                Substitute.For<IEventBus>(),
                dispatcher,
                TimeProvider.System,
                NullLogger<ToolCallExecutor>.Instance));
        var context = PipelineTestContext.Create();
        context.SessionId = new SessionId("alice", SessionId.DefaultSessionName);
        context.Outcome = new IterationOutcome(
            Interrupted: true,
            ToolResultTurns: [],
            ToolCalls: [new ToolCall("test", "noop", "{}", "1")]);

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await Assert.That(context.Outcome!.Interrupted).IsTrue();
        await Assert.That(context.Outcome.ToolResultTurns.Length).IsEqualTo(1);
        await Assert.That(context.Outcome.ToolResultTurns[0].IsError).IsTrue();
    }
}
