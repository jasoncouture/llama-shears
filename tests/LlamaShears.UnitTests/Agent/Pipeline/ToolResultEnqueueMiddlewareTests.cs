using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Pipeline;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class ToolResultEnqueueMiddlewareTests
{
    [Test]
    public async Task EnqueuesToolTurnsWhenTheOutcomeIsNotInterrupted()
    {
        var queue = Substitute.For<ISessionQueue>();
        IAgentMiddleware middleware = new ToolResultEnqueueMiddleware(queue);
        var toolTurn = new ModelTurn(ModelRole.Tool, "ok", DateTimeOffset.UnixEpoch);
        var context = PipelineTestContext.Create();

        await middleware.InvokeAsync(
            context,
            (ctx, _) =>
            {
                ctx.Outcome = new IterationOutcome(Interrupted: false, ToolResultTurns: [toolTurn]);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await queue.Received(1).EnqueueAsync(toolTurn, context.ShutdownToken);
    }

    [Test]
    public async Task SkipsEnqueueWhenInterrupted()
    {
        var queue = Substitute.For<ISessionQueue>();
        IAgentMiddleware middleware = new ToolResultEnqueueMiddleware(queue);
        var toolTurn = new ModelTurn(ModelRole.Tool, "ok", DateTimeOffset.UnixEpoch);
        var context = PipelineTestContext.Create();

        await middleware.InvokeAsync(
            context,
            (ctx, _) =>
            {
                ctx.Outcome = new IterationOutcome(Interrupted: true, ToolResultTurns: [toolTurn]);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await queue.DidNotReceive().EnqueueAsync(Arg.Any<ModelTurn>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SkipsEnqueueWhenOutcomeIsMissing()
    {
        var queue = Substitute.For<ISessionQueue>();
        IAgentMiddleware middleware = new ToolResultEnqueueMiddleware(queue);

        await middleware.InvokeAsync(
            PipelineTestContext.Create(),
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        await queue.DidNotReceive().EnqueueAsync(Arg.Any<ModelTurn>(), Arg.Any<CancellationToken>());
    }
}
