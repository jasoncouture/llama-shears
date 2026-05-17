using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentLockManagerTests
{
    [Test]
    public async Task AcquireLockAsyncReturnsActiveScope()
    {
        var manager = new AgentLockManager();

        using var scope = await manager.AcquireLockAsync("alice", CancellationToken.None);

        await Assert.That(scope.Active).IsTrue();
    }

    [Test]
    public async Task DisposingScopeClearsActive()
    {
        var manager = new AgentLockManager();
        var scope = await manager.AcquireLockAsync("alice", CancellationToken.None);

        scope.Dispose();

        await Assert.That(scope.Active).IsFalse();
    }

    [Test]
    public async Task SecondAcquireForSameAgentBlocksUntilFirstScopeIsDisposed()
    {
        var manager = new AgentLockManager();
        var first = await manager.AcquireLockAsync("alice", CancellationToken.None);

        var secondTask = manager.AcquireLockAsync("alice", CancellationToken.None).AsTask();
        await Task.Delay(50);
        await Assert.That(secondTask.IsCompleted).IsFalse();

        first.Dispose();

        using var second = await secondTask.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(second.Active).IsTrue();
    }

    [Test]
    public async Task LocksForDifferentAgentsAreIndependent()
    {
        var manager = new AgentLockManager();

        using var alice = await manager.AcquireLockAsync("alice", CancellationToken.None);
        using var bob = await manager.AcquireLockAsync("bob", CancellationToken.None);

        await Assert.That(alice.Active).IsTrue();
        await Assert.That(bob.Active).IsTrue();
    }

    [Test]
    public async Task DisposingScopeTwiceIsANoOp()
    {
        var manager = new AgentLockManager();
        var scope = await manager.AcquireLockAsync("alice", CancellationToken.None);

        scope.Dispose();
        scope.Dispose();

        using var next = await manager.AcquireLockAsync("alice", CancellationToken.None)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(next.Active).IsTrue();
    }

    [Test]
    public async Task CancellationTokenCancelsAPendingAcquire()
    {
        var manager = new AgentLockManager();
        using var held = await manager.AcquireLockAsync("alice", CancellationToken.None);

        using var cancellationTokenSource = new CancellationTokenSource();
        var pending = manager.AcquireLockAsync("alice", cancellationTokenSource.Token).AsTask();
        cancellationTokenSource.Cancel();

        await Assert.That(async () => await pending).Throws<OperationCanceledException>();
    }

    [Test]
    public async Task DroppingScopeWithoutDisposeReleasesSemaphoreViaFinalizer()
    {
        var manager = new AgentLockManager();
        await AcquireAndAbandonAsync(manager);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        using var next = await manager.AcquireLockAsync("alice", CancellationToken.None)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(next.Active).IsTrue();
    }

    private static async Task AcquireAndAbandonAsync(AgentLockManager manager)
    {
        _ = await manager.AcquireLockAsync("alice", CancellationToken.None);
    }
}
