using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Pipeline;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class ActiveTurnCancellationTests
{
    [Test]
    public async Task CancelTripsTheRegisteredSource()
    {
        IActiveTurnCancellation slot = new ActiveTurnCancellation();
        using var source = new CancellationTokenSource();
        slot.Register(source);

        slot.Cancel();

        await Assert.That(source.IsCancellationRequested).IsTrue();
    }

    [Test]
    public async Task UnregisterLeavesALaterSourceIntact()
    {
        IActiveTurnCancellation slot = new ActiveTurnCancellation();
        using var first = new CancellationTokenSource();
        using var second = new CancellationTokenSource();
        slot.Register(first);
        slot.Register(second);
        slot.Unregister(first);

        slot.Cancel();

        await Assert.That(first.IsCancellationRequested).IsFalse();
        await Assert.That(second.IsCancellationRequested).IsTrue();
    }

    [Test]
    public async Task CancelWithNoRegistrationIsANoOp()
    {
        IActiveTurnCancellation slot = new ActiveTurnCancellation();
        slot.Cancel();
        await Assert.That(slot).IsNotNull();
    }
}
