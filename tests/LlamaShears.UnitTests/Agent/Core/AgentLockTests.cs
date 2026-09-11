using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentLockTests
{
    [Test]
    public async Task AcquireLockAsyncForwardsTheCurrentSessionIdToTheManager()
    {
        var manager = new AgentLockManager();
        var session = new SessionId("alice", SessionId.DefaultSessionName);
        var agentLock = BindLock(manager, session);

        using var locked = await agentLock.AcquireLockAsync(CancellationToken.None);

        await Assert.That(locked.Active).IsTrue();

        var pending = manager.AcquireLockAsync(session.ToString(), CancellationToken.None).AsTask();
        await Task.Delay(50);
        await Assert.That(pending.IsCompleted).IsFalse();

        locked.Dispose();
        using var next = await pending.WaitAsync(TimeSpan.FromMilliseconds(500));
        await Assert.That(next.Active).IsTrue();
    }

    [Test]
    public async Task TwoLocksBoundToDifferentAgentsUseIndependentSemaphores()
    {
        var manager = new AgentLockManager();
        var aliceLock = BindLock(manager, new SessionId("alice", SessionId.DefaultSessionName));
        var bobLock = BindLock(manager, new SessionId("bob", SessionId.DefaultSessionName));

        using var aliceScopeHandle = await aliceLock.AcquireLockAsync(CancellationToken.None);
        using var bobScopeHandle = await bobLock.AcquireLockAsync(CancellationToken.None);

        await Assert.That(aliceScopeHandle.Active).IsTrue();
        await Assert.That(bobScopeHandle.Active).IsTrue();
    }

    [Test]
    public async Task TwoSessionsOfTheSameAgentUseIndependentSemaphores()
    {
        var manager = new AgentLockManager();
        var parent = new SessionId("alice", SessionId.DefaultSessionName);
        var child = new SessionId("alice", "subagent-1");
        var parentLock = BindLock(manager, parent);
        var childLock = BindLock(manager, child);

        using var parentHeld = await parentLock.AcquireLockAsync(CancellationToken.None);
        using var childHeld = await childLock.AcquireLockAsync(CancellationToken.None);

        await Assert.That(parentHeld.Active).IsTrue();
        await Assert.That(childHeld.Active).IsTrue();
    }

    [Test]
    public async Task SameSessionStillSerializes()
    {
        var manager = new AgentLockManager();
        var session = new SessionId("alice", SessionId.DefaultSessionName);
        var firstLock = BindLock(manager, session);
        var secondLock = BindLock(manager, session);

        using var held = await firstLock.AcquireLockAsync(CancellationToken.None);
        var pending = secondLock.AcquireLockAsync(CancellationToken.None).AsTask();
        await Task.Delay(50);
        await Assert.That(pending.IsCompleted).IsFalse();

        held.Dispose();
        using var next = await pending.WaitAsync(TimeSpan.FromMilliseconds(500));
        await Assert.That(next.Active).IsTrue();
    }

    private static IAgentLock BindLock(IAgentLockManager manager, SessionId session)
    {
        IDataContextScope scope = new FakeDataContextScope(session);
        scope.SetItem(AgentConfig.DataKey, TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, session.AgentId));
        scope.SetItem(SessionPath.DataKey, new SessionPath(session));
        return new AgentLock(manager, scope);
    }
}
