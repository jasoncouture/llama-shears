using LlamaShears.Api.Web.Services;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.UnitTests.Agent.Core;
using NSubstitute;

namespace LlamaShears.UnitTests.Services;

public sealed class DataContextInspectorTests
{
    [Test]
    public async Task InspectReturnsEmptyWhenNoScopeIsAlive()
    {
        var session = SessionId.CreateFor("alice");
        var factory = Substitute.For<IDataContextFactory>();
        factory.Current.Returns((IDataContextScope?)null);
        factory.TryJoinContextScope(session, out Arg.Any<IDataContextScope?>()).Returns(false);
        IDataContextInspector inspector = new DataContextInspector(factory);

        var inspection = inspector.Inspect(session);

        await Assert.That(inspection.SessionId).IsEqualTo(session.ToString());
        await Assert.That(inspection.Entries).IsEmpty();
        await Assert.That(inspection.Error).IsEqualTo("No live data context for this session.");
        factory.DidNotReceive().ClearCurrent(Arg.Any<bool>());
    }

    [Test]
    public async Task InspectSnapshotsEntriesAndClearsAJoinedScope()
    {
        var session = SessionId.CreateFor("alice");
        var scope = new FakeDataContextScope(session);
        scope.SetItems(
        [
            new KeyValuePair<string, object?>("zeta", 1),
            new KeyValuePair<string, object?>("Alpha", new SamplePayload("x", 2)),
        ]);
        var factory = Substitute.For<IDataContextFactory>();
        factory.Current.Returns((IDataContextScope?)null);
        factory.TryJoinContextScope(session, out Arg.Any<IDataContextScope?>())
            .Returns(call =>
            {
                call[1] = scope;
                return true;
            });
        IDataContextInspector inspector = new DataContextInspector(factory);

        var inspection = inspector.Inspect(session);

        await Assert.That(inspection.Error).IsNull();
        await Assert.That(inspection.Entries.Length).IsEqualTo(2);
        await Assert.That(inspection.Entries[0].Key).IsEqualTo("Alpha");
        await Assert.That(inspection.Entries[0].TypeName).IsEqualTo(nameof(SamplePayload));
        await Assert.That(inspection.Entries[0].Value).Contains("\"Name\": \"x\"");
        await Assert.That(inspection.Entries[1].Key).IsEqualTo("zeta");
        await Assert.That(inspection.Entries[1].TypeName).IsEqualTo(nameof(Int32));
        await Assert.That(inspection.Entries[1].Value).IsEqualTo("1");
        factory.Received(1).ClearCurrent(false);
    }

    [Test]
    public async Task InspectDoesNotClearWhenTheScopeIsAlreadyCurrent()
    {
        var session = SessionId.CreateFor("alice");
        var scope = new FakeDataContextScope(session);
        scope.SetItems([new KeyValuePair<string, object?>("ready", true)]);
        var factory = Substitute.For<IDataContextFactory>();
        factory.Current.Returns(scope);
        factory.TryJoinContextScope(session, out Arg.Any<IDataContextScope?>())
            .Returns(call =>
            {
                call[1] = scope;
                return true;
            });
        IDataContextInspector inspector = new DataContextInspector(factory);

        var inspection = inspector.Inspect(session);

        await Assert.That(inspection.Error).IsNull();
        await Assert.That(inspection.Entries.Length).IsEqualTo(1);
        factory.DidNotReceive().ClearCurrent(Arg.Any<bool>());
    }

    [Test]
    public async Task InspectSurfacesAJoinConflictWithoutThrowing()
    {
        var session = SessionId.CreateFor("alice");
        var factory = Substitute.For<IDataContextFactory>();
        factory.TryJoinContextScope(session, out Arg.Any<IDataContextScope?>())
            .Returns(_ => throw new InvalidOperationException(
                "A scope is already present on this call chain; cannot join a different scope."));
        IDataContextInspector inspector = new DataContextInspector(factory);

        var inspection = inspector.Inspect(session);

        await Assert.That(inspection.Entries).IsEmpty();
        await Assert.That(inspection.Error)
            .IsEqualTo("A scope is already present on this call chain; cannot join a different scope.");
        factory.DidNotReceive().ClearCurrent(Arg.Any<bool>());
    }

    private sealed record SamplePayload(string Name, int Count);
}
