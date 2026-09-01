using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class TurnExceptionMiddlewareTests
{
    [Test]
    public async Task InterruptCancellationIsSwallowed()
    {
        IAgentMiddleware middleware = new TurnExceptionMiddleware(
            NullLogger<TurnExceptionMiddleware>.Instance,
            PipelineTestContext.ScopeFor());
        using var shutdown = new CancellationTokenSource();
        using var turn = new CancellationTokenSource();
        turn.Cancel();
        var context = PipelineTestContext.Create(shutdown.Token);
        context.TurnToken = turn.Token;

        await middleware.InvokeAsync(
            context,
            (_, _) => throw new OperationCanceledException(turn.Token),
            shutdown.Token);
    }

    [Test]
    public async Task ShutdownCancellationPropagates()
    {
        IAgentMiddleware middleware = new TurnExceptionMiddleware(
            NullLogger<TurnExceptionMiddleware>.Instance,
            PipelineTestContext.ScopeFor());
        using var shutdown = new CancellationTokenSource();
        shutdown.Cancel();
        var context = PipelineTestContext.Create(shutdown.Token);

        await Assert.That(() => middleware.InvokeAsync(
                context,
                (_, _) => throw new OperationCanceledException(shutdown.Token),
                shutdown.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task UnexpectedFailureIsSwallowedSoTheLoopSurvives()
    {
        IAgentMiddleware middleware = new TurnExceptionMiddleware(
            NullLogger<TurnExceptionMiddleware>.Instance,
            PipelineTestContext.ScopeFor());
        var context = PipelineTestContext.Create();

        await middleware.InvokeAsync(
            context,
            (_, _) => throw new InvalidOperationException("boom"),
            CancellationToken.None);
    }

    [Test]
    public async Task InterruptedOutcomeDoesNotThrow()
    {
        IAgentMiddleware middleware = new TurnExceptionMiddleware(
            NullLogger<TurnExceptionMiddleware>.Instance,
            PipelineTestContext.ScopeFor());
        var context = PipelineTestContext.Create();

        await middleware.InvokeAsync(
            context,
            (ctx, _) =>
            {
                ctx.Outcome = new IterationOutcome(Interrupted: true, ToolResultTurns: []);
                return Task.CompletedTask;
            },
            CancellationToken.None);
    }
}
