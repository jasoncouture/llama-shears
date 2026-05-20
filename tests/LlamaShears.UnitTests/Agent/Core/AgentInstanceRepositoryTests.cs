using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentInstanceRepositoryTests
{
    [Test]
    public async Task AddAgentRootSessionSucceedsAndIsRetrievable()
    {
        var repository = new AgentInstanceRepository();
        var rootHandle = BuildHandle(RootPath("agent-a"));

        repository.AddAgent(rootHandle);

        await Assert.That(repository.TryGetAgent(rootHandle.SessionPath.Id, out var found)).IsTrue();
        await Assert.That(found).IsSameReferenceAs(rootHandle);
    }

    [Test]
    public async Task AddAgentDuplicateIdThrows()
    {
        var repository = new AgentInstanceRepository();
        var path = RootPath("agent-a");
        repository.AddAgent(BuildHandle(path));

        var duplicate = BuildHandle(path);
        await Assert.That(() => repository.AddAgent(duplicate))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AddAgentChildBeforeParentThrows()
    {
        var repository = new AgentInstanceRepository();
        var rootPath = RootPath("agent-a");
        var childPath = rootPath.CreateChildSession(new SessionId("agent-a", "child"));

        await Assert.That(() => repository.AddAgent(BuildHandle(childPath)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AddAgentChildOfRootSucceeds()
    {
        var repository = new AgentInstanceRepository();
        var rootPath = RootPath("agent-a");
        repository.AddAgent(BuildHandle(rootPath));

        var childPath = rootPath.CreateChildSession(new SessionId("agent-a", "child"));
        var childHandle = BuildHandle(childPath);
        repository.AddAgent(childHandle);

        await Assert.That(repository.TryGetAgent(childHandle.SessionPath.Id, out var found)).IsTrue();
        await Assert.That(found).IsSameReferenceAs(childHandle);
    }

    [Test]
    public async Task AddAgentAfterDisposeThrowsObjectDisposed()
    {
        var repository = new AgentInstanceRepository();
        repository.Dispose();

        await Assert.That(() => repository.AddAgent(BuildHandle(RootPath("agent-a"))))
            .Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task TryGetAgentMissReturnsFalseAndNullHandle()
    {
        var repository = new AgentInstanceRepository();

        var found = repository.TryGetAgent(Guid.NewGuid(), out var handle);

        await Assert.That(found).IsFalse();
        await Assert.That(handle).IsNull();
    }

    [Test]
    public async Task GetAgentMissThrowsKeyNotFound()
    {
        var repository = new AgentInstanceRepository();

        await Assert.That(() => repository.GetAgent(Guid.NewGuid()))
            .Throws<KeyNotFoundException>();
    }

    [Test]
    public async Task RemoveLeafReturnsTrueAndYieldsHandle()
    {
        var repository = new AgentInstanceRepository();
        var rootHandle = BuildHandle(RootPath("agent-a"));
        repository.AddAgent(rootHandle);

        var removed = repository.Remove(rootHandle.SessionPath.Id, out var handle);

        await Assert.That(removed).IsTrue();
        await Assert.That(handle).IsSameReferenceAs(rootHandle);
        await Assert.That(repository.TryGetAgent(rootHandle.SessionPath.Id, out _)).IsFalse();
    }

    [Test]
    public async Task RemoveAgentWithChildrenThrowsInvalidOperation()
    {
        var repository = new AgentInstanceRepository();
        var rootHandle = BuildHandle(RootPath("agent-a"));
        repository.AddAgent(rootHandle);
        var childHandle = BuildHandle(rootHandle.SessionPath.CreateChildSession(new SessionId("agent-a", "child")));
        repository.AddAgent(childHandle);

        await Assert.That(() => repository.Remove(rootHandle.SessionPath.Id, out _))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TryRemoveAgentWithChildrenReturnsFalse()
    {
        var repository = new AgentInstanceRepository();
        var rootHandle = BuildHandle(RootPath("agent-a"));
        repository.AddAgent(rootHandle);
        var childHandle = BuildHandle(rootHandle.SessionPath.CreateChildSession(new SessionId("agent-a", "child")));
        repository.AddAgent(childHandle);

        var removed = repository.TryRemove(rootHandle.SessionPath.Id, out var handle);

        await Assert.That(removed).IsFalse();
        await Assert.That(handle).IsNull();
        await Assert.That(repository.TryGetAgent(rootHandle.SessionPath.Id, out _)).IsTrue();
    }

    [Test]
    public async Task TryRemoveLeafSucceeds()
    {
        var repository = new AgentInstanceRepository();
        var rootHandle = BuildHandle(RootPath("agent-a"));
        repository.AddAgent(rootHandle);
        var childHandle = BuildHandle(rootHandle.SessionPath.CreateChildSession(new SessionId("agent-a", "child")));
        repository.AddAgent(childHandle);

        var removed = repository.TryRemove(childHandle.SessionPath.Id, out var handle);

        await Assert.That(removed).IsTrue();
        await Assert.That(handle).IsSameReferenceAs(childHandle);
    }

    [Test]
    public async Task DescendentsOfReturnsLeavesBeforeParents()
    {
        var repository = new AgentInstanceRepository();
        var rootHandle = BuildHandle(RootPath("agent-a"));
        repository.AddAgent(rootHandle);
        var childHandle = BuildHandle(rootHandle.SessionPath.CreateChildSession(new SessionId("agent-a", "child")));
        repository.AddAgent(childHandle);
        var grandHandle = BuildHandle(childHandle.SessionPath.CreateChildSession(new SessionId("agent-a", "grand")));
        repository.AddAgent(grandHandle);

        var descendants = repository.DescendentsOf(rootHandle.SessionPath.Id).ToList();

        await Assert.That(descendants.Count).IsEqualTo(2);
        await Assert.That(descendants[0]).IsSameReferenceAs(grandHandle);
        await Assert.That(descendants[1]).IsSameReferenceAs(childHandle);
    }

    [Test]
    public async Task DescendentsOfUnknownParentReturnsEmpty()
    {
        var repository = new AgentInstanceRepository();

        var descendants = repository.DescendentsOf(Guid.NewGuid()).ToList();

        await Assert.That(descendants.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetAllAgentsYieldsEachRootAndDescendant()
    {
        var repository = new AgentInstanceRepository();
        var rootA = BuildHandle(RootPath("agent-a"));
        var rootB = BuildHandle(RootPath("agent-b"));
        repository.AddAgent(rootA);
        repository.AddAgent(rootB);
        var childA = BuildHandle(rootA.SessionPath.CreateChildSession(new SessionId("agent-a", "child")));
        repository.AddAgent(childA);

        var allIds = repository.GetAllAgents().Select(h => h.SessionPath.Id).ToHashSet();

        await Assert.That(allIds.Count).IsEqualTo(3);
        await Assert.That(allIds.Contains(rootA.SessionPath.Id)).IsTrue();
        await Assert.That(allIds.Contains(rootB.SessionPath.Id)).IsTrue();
        await Assert.That(allIds.Contains(childA.SessionPath.Id)).IsTrue();
    }

    [Test]
    public async Task GetAgentInstancesByNameMatchesOnSessionNameCaseInsensitively()
    {
        var repository = new AgentInstanceRepository();
        var rootA = BuildHandle(RootPath("agent-a"));
        var rootB = BuildHandle(RootPath("agent-b"));
        repository.AddAgent(rootA);
        repository.AddAgent(rootB);
        var childA = BuildHandle(rootA.SessionPath.CreateChildSession(new SessionId("agent-a", "Worker")));
        repository.AddAgent(childA);

        var byDefault = repository.GetAgentInstancesByName(SessionId.DefaultSessionName).ToHashSet();
        var byWorker = repository.GetAgentInstancesByName("worker").ToHashSet();

        await Assert.That(byDefault.Contains(rootA.SessionPath.Id)).IsTrue();
        await Assert.That(byDefault.Contains(rootB.SessionPath.Id)).IsTrue();
        await Assert.That(byDefault.Contains(childA.SessionPath.Id)).IsFalse();
        await Assert.That(byWorker.Count).IsEqualTo(1);
        await Assert.That(byWorker.Contains(childA.SessionPath.Id)).IsTrue();
    }

    [Test]
    public async Task RemoveDescendentsClearsSubtree()
    {
        var repository = new AgentInstanceRepository();
        var rootHandle = BuildHandle(RootPath("agent-a"));
        repository.AddAgent(rootHandle);
        var childHandle = BuildHandle(rootHandle.SessionPath.CreateChildSession(new SessionId("agent-a", "child")));
        repository.AddAgent(childHandle);
        var grandHandle = BuildHandle(childHandle.SessionPath.CreateChildSession(new SessionId("agent-a", "grand")));
        repository.AddAgent(grandHandle);

        repository.RemoveDescendents(rootHandle.SessionPath.Id);

        await Assert.That(repository.TryGetAgent(grandHandle.SessionPath.Id, out _)).IsFalse();
        await Assert.That(repository.TryGetAgent(childHandle.SessionPath.Id, out _)).IsFalse();
        await Assert.That(repository.TryGetAgent(rootHandle.SessionPath.Id, out _)).IsTrue();
    }

    private static SessionPath RootPath(string agentId)
    {
        return new SessionPath(new SessionId(agentId, SessionId.DefaultSessionName));
    }

    private static AgentHandle BuildHandle(SessionPath path)
    {
        var scope = new AsyncServiceScope(Substitute.For<IServiceScope>());
        return new AgentHandle(path, "hash", scope, ExecutionContext.Capture()!);
    }
}
