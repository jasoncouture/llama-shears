using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Paths;
using LlamaShears.Core.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace LlamaShears.UnitTests.Agent.Core.Persistence;

public sealed class JsonLineContextStoreTests
{
    private const string AgentId = "alice";

    private static ModelTurn Turn(ModelRole role, string content, DateTimeOffset at) =>
        new(role, content, at);

    [Test]
    public async Task AppendThenReadCurrentRoundTripsModelTurnViaPolymorphicJson()
    {
        using var fixture = new Fixture();
        var context = await fixture.Store.OpenAsync(AgentId, CancellationToken.None);

        var first = Turn(ModelRole.User, "hi", new DateTimeOffset(2026, 4, 29, 20, 0, 0, TimeSpan.Zero));
        var second = Turn(ModelRole.Assistant, "hello", new DateTimeOffset(2026, 4, 29, 20, 0, 1, TimeSpan.Zero));

        await context.AppendAsync(first, CancellationToken.None);
        await context.AppendAsync(second, CancellationToken.None);

        var read = new List<IContextEntry>();
        await foreach (var entry in fixture.Store.ReadCurrentAsync(AgentId, CancellationToken.None))
        {
            read.Add(entry);
        }

        await Assert.That(read.Count).IsEqualTo(2);
        await Assert.That(read[0]).IsEqualTo(first);
        await Assert.That(read[1]).IsEqualTo(second);
    }

    [Test]
    public async Task ReadCurrentSkipsMalformedLines()
    {
        using var fixture = new Fixture();
        var context = await fixture.Store.OpenAsync(AgentId, CancellationToken.None);
        await context.AppendAsync(
            Turn(ModelRole.User, "valid", DateTimeOffset.UnixEpoch),
            CancellationToken.None);

        await File.AppendAllTextAsync(
            fixture.CurrentPath(AgentId),
            "this is not json\n",
            CancellationToken.None);
        await File.AppendAllTextAsync(
            fixture.CurrentPath(AgentId),
            "{\"kind\":\"unknown\"}\n",
            CancellationToken.None);

        var read = new List<IContextEntry>();
        await foreach (var entry in fixture.Store.ReadCurrentAsync(AgentId, CancellationToken.None))
        {
            read.Add(entry);
        }

        await Assert.That(read.Count).IsEqualTo(1);
        await Assert.That(read[0]).IsTypeOf<ModelTurn>();
    }

    [Test]
    public async Task RepeatedOpenAsyncReturnsTheSameHandle()
    {
        using var fixture = new Fixture();
        var first = await fixture.Store.OpenAsync(AgentId, CancellationToken.None);
        var second = await fixture.Store.OpenAsync(AgentId, CancellationToken.None);
        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    [Test]
    public async Task AppendIsVisibleThroughBothTurnsAndEntries()
    {
        using var fixture = new Fixture();
        var context = await fixture.Store.OpenAsync(AgentId, CancellationToken.None);
        var turn = Turn(ModelRole.User, "x", DateTimeOffset.UnixEpoch);

        await context.AppendAsync(turn, CancellationToken.None);

        await Assert.That(context.Turns).Contains(turn);
        await Assert.That(context.Entries).Contains((IContextEntry)turn);
    }

    [Test]
    public async Task OpenHydratesFromExistingCurrentJson()
    {
        using var fixture = new Fixture();

        var first = await fixture.Store.OpenAsync(AgentId, CancellationToken.None);
        var existing = Turn(ModelRole.User, "remembered", DateTimeOffset.UnixEpoch);
        await first.AppendAsync(existing, CancellationToken.None);

        // New store instance over the same data root simulates a process restart.
        var rebooted = fixture.NewStore();
        var rehydrated = await rebooted.OpenAsync(AgentId, CancellationToken.None);

        await Assert.That(rehydrated.Turns).Contains(existing);
    }

    [Test]
    public async Task ClearWithArchiveRenamesCurrentAndEmptiesInMemory()
    {
        using var fixture = new Fixture();
        var context = await fixture.Store.OpenAsync(AgentId, CancellationToken.None);
        await context.AppendAsync(
            Turn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch),
            CancellationToken.None);

        fixture.TimeProvider.SetUtcNow(new DateTimeOffset(2026, 4, 29, 20, 0, 0, TimeSpan.Zero));
        var expectedMillis = fixture.TimeProvider.GetUtcNow().ToUnixTimeMilliseconds();

        await fixture.Store.ClearAsync(AgentId, archive: true, CancellationToken.None);

        var archivePath = Path.Combine(fixture.AgentFolder(AgentId), $"{expectedMillis}.json");
        await Assert.That(File.Exists(archivePath)).IsTrue();
        await Assert.That(File.Exists(fixture.CurrentPath(AgentId))).IsFalse();
        await Assert.That(context.Turns.Count).IsEqualTo(0);
        await Assert.That(Directory.Exists(fixture.AgentFolder(AgentId))).IsTrue();
    }

    [Test]
    public async Task ClearWithoutArchiveDeletesCurrentAndEmptiesInMemory()
    {
        using var fixture = new Fixture();
        var context = await fixture.Store.OpenAsync(AgentId, CancellationToken.None);
        await context.AppendAsync(
            Turn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch),
            CancellationToken.None);

        await fixture.Store.ClearAsync(AgentId, archive: false, CancellationToken.None);

        await Assert.That(File.Exists(fixture.CurrentPath(AgentId))).IsFalse();
        await Assert.That(context.Turns.Count).IsEqualTo(0);
        await Assert.That(Directory.Exists(fixture.AgentFolder(AgentId))).IsTrue();
    }

    [Test]
    public async Task ClearAsyncRaisesClearedEventOnTheLiveContext()
    {
        using var fixture = new Fixture();
        var context = await fixture.Store.OpenAsync(AgentId, CancellationToken.None);
        var raised = 0;
        context.Cleared += (_, _) => Interlocked.Increment(ref raised);

        await fixture.Store.ClearAsync(AgentId, archive: false, CancellationToken.None);

        await Assert.That(raised).IsEqualTo(1);
    }

    [Test]
    public async Task ListArchivesAsyncReturnsArchivesInChronologicalOrder()
    {
        using var fixture = new Fixture();
        var context = await fixture.Store.OpenAsync(AgentId, CancellationToken.None);

        async Task ArchiveAt(long unixMillis)
        {
            fixture.TimeProvider.SetUtcNow(DateTimeOffset.FromUnixTimeMilliseconds(unixMillis));
            await context.AppendAsync(
                Turn(ModelRole.User, $"#{unixMillis}", DateTimeOffset.UnixEpoch),
                CancellationToken.None);
            await fixture.Store.ClearAsync(AgentId, archive: true, CancellationToken.None);
        }

        await ArchiveAt(1000);
        await ArchiveAt(2000);
        await ArchiveAt(3000);

        var archives = await fixture.Store.ListArchivesAsync(AgentId, CancellationToken.None);

        await Assert.That(archives.Select(a => a.UnixMillis).ToArray())
            .IsEquivalentTo(new long[] { 1000, 2000, 3000 });
    }

    [Test]
    public async Task DeleteAsyncRemovesOnlyTheTargetedArchive()
    {
        using var fixture = new Fixture();
        var context = await fixture.Store.OpenAsync(AgentId, CancellationToken.None);

        async Task<ArchiveId> ArchiveAt(long unixMillis)
        {
            fixture.TimeProvider.SetUtcNow(DateTimeOffset.FromUnixTimeMilliseconds(unixMillis));
            await context.AppendAsync(
                Turn(ModelRole.User, $"#{unixMillis}", DateTimeOffset.UnixEpoch),
                CancellationToken.None);
            await fixture.Store.ClearAsync(AgentId, archive: true, CancellationToken.None);
            return new ArchiveId(AgentId, unixMillis);
        }

        var keep = await ArchiveAt(1000);
        var drop = await ArchiveAt(2000);

        await fixture.Store.DeleteAsync(drop, CancellationToken.None);

        var remaining = await fixture.Store.ListArchivesAsync(AgentId, CancellationToken.None);
        await Assert.That(remaining.Count).IsEqualTo(1);
        await Assert.That(remaining[0]).IsEqualTo(keep);
        await Assert.That(Directory.Exists(fixture.AgentFolder(AgentId))).IsTrue();
    }

    [Test]
    public async Task AppendRecreatesCurrentJsonAfterExternalDelete()
    {
        using var fixture = new Fixture();
        var context = await fixture.Store.OpenAsync(AgentId, CancellationToken.None);
        await context.AppendAsync(
            Turn(ModelRole.User, "first", DateTimeOffset.UnixEpoch),
            CancellationToken.None);

        File.Delete(fixture.CurrentPath(AgentId));

        await context.AppendAsync(
            Turn(ModelRole.User, "second", DateTimeOffset.UnixEpoch),
            CancellationToken.None);

        await Assert.That(File.Exists(fixture.CurrentPath(AgentId))).IsTrue();

        var lines = await File.ReadAllLinesAsync(
            fixture.CurrentPath(AgentId),
            CancellationToken.None);
        await Assert.That(lines.Length).IsEqualTo(1);
        await Assert.That(lines[0]).Contains("second");
    }

    [Test]
    public async Task ListAgentsAsyncReturnsAgentsWithFolders()
    {
        using var fixture = new Fixture();
        await fixture.Store.OpenAsync("alice", CancellationToken.None);
        await fixture.Store.OpenAsync("bob", CancellationToken.None);

        var agents = await fixture.Store.ListAgentsAsync(CancellationToken.None);

        await Assert.That(agents).IsEquivalentTo(new[] { "alice", "bob" });
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _root;

        public Fixture()
        {
            _root = Path.Combine(Path.GetTempPath(), $"llamashears-store-{Guid.NewGuid():N}");
            Paths = new ShearsPaths(Options.Create(new ShearsPathsOptions { DataRoot = _root }));
            TimeProvider = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
            Store = new JsonLineContextStore(Paths, TimeProvider);
        }

        public IShearsPaths Paths { get; }

        public FakeTimeProvider TimeProvider { get; }

        public JsonLineContextStore Store { get; }

        public JsonLineContextStore NewStore() => new(Paths, TimeProvider);

        public string AgentFolder(string agentId) =>
            Paths.GetPath(PathKind.Context, agentId, ensureExists: true);

        public string CurrentPath(string agentId) =>
            Path.Combine(AgentFolder(agentId), "current.json");

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
