using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentLockTests
{
    [Test]
    public async Task AcquireLockAsyncForwardsTheCurrentAgentIdToTheManager()
    {
        var manager = new AgentLockManager();
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice");
        IDataContextScope scope = new FakeDataContextScope("alice");
        scope.SetItem(AgentConfig.DataKey, config);
        var agentLock = new AgentLock(manager, scope);

        using var locked = await agentLock.AcquireLockAsync(CancellationToken.None);

        await Assert.That(locked.Active).IsTrue();

        var pending = manager.AcquireLockAsync("alice", CancellationToken.None).AsTask();
        await Task.Delay(50);
        await Assert.That(pending.IsCompleted).IsFalse();

        locked.Dispose();
        using var next = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(next.Active).IsTrue();
    }

    [Test]
    public async Task TwoLocksBoundToDifferentScopesUseIndependentSemaphores()
    {
        var manager = new AgentLockManager();
        IDataContextScope aliceScope = new FakeDataContextScope("alice");
        aliceScope.SetItem(AgentConfig.DataKey, TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice"));
        IDataContextScope bobScope = new FakeDataContextScope("bob");
        bobScope.SetItem(AgentConfig.DataKey, TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "bob"));
        var aliceLock = new AgentLock(manager, aliceScope);
        var bobLock = new AgentLock(manager, bobScope);

        using var aliceScopeHandle = await aliceLock.AcquireLockAsync(CancellationToken.None);
        using var bobScopeHandle = await bobLock.AcquireLockAsync(CancellationToken.None);

        await Assert.That(aliceScopeHandle.Active).IsTrue();
        await Assert.That(bobScopeHandle.Active).IsTrue();
    }
}
