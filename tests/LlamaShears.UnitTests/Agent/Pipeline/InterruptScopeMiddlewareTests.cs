using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Pipeline;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class InterruptScopeMiddlewareTests
{
    [Test]
    public async Task ReplacesTurnTokenAndExposesItToCancel()
    {
        IActiveTurnCancellation slot = new ActiveTurnCancellation();
        IAgentMiddleware middleware = new InterruptScopeMiddleware(slot);
        using var shutdown = new CancellationTokenSource();
        var context = PipelineTestContext.Create(shutdown.Token);
        CancellationToken turnSeen = default;

        await middleware.InvokeAsync(
            context,
            (_, _) =>
            {
                turnSeen = context.TurnToken;
                slot.Cancel();
                return Task.CompletedTask;
            },
            shutdown.Token);

        await Assert.That(turnSeen.IsCancellationRequested).IsTrue();
        await Assert.That(shutdown.IsCancellationRequested).IsFalse();
    }

    [Test]
    public async Task UnregistersTheSourceAfterNext()
    {
        IActiveTurnCancellation slot = new ActiveTurnCancellation();
        IAgentMiddleware middleware = new InterruptScopeMiddleware(slot);
        using var leftover = new CancellationTokenSource();

        await middleware.InvokeAsync(
            PipelineTestContext.Create(),
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        slot.Register(leftover);
        slot.Cancel();
        await Assert.That(leftover.IsCancellationRequested).IsTrue();
    }
}
