using System.Text.Json;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Cron;
using LlamaShears.Core.Paths;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LlamaShears.UnitTests.Cron;

public sealed class JsonCronStoreTests
{
    [Test]
    public async Task GetAllOnFreshStoreReturnsEmpty()
    {
        using var fixture = new TempRoot();
        var store = NewStore(fixture);

        var jobs = await store.GetAllAsync();

        await Assert.That(jobs).IsEmpty();
    }

    [Test]
    public async Task UpsertPersistsAcrossInstances()
    {
        using var fixture = new TempRoot();
        var first = NewStore(fixture);

        var job = NewJob("agent-a", "nightly", "0 0 * * *", "wake up");
        await first.UpsertAsync(job);

        var second = NewStore(fixture);
        var roundTripped = await second.GetAsync(job.Id);

        await Assert.That(roundTripped).IsNotNull();
        await Assert.That(roundTripped!.Name).IsEqualTo("nightly");
        await Assert.That(roundTripped.AgentId).IsEqualTo("agent-a");
        await Assert.That(roundTripped.CronExpression).IsEqualTo("0 0 * * *");
        await Assert.That(roundTripped.Prompt).IsEqualTo("wake up");
    }

    [Test]
    public async Task UpsertReplacesExistingByIdNotByContent()
    {
        using var fixture = new TempRoot();
        var store = NewStore(fixture);

        var job = NewJob("agent-a", "v1", "0 0 * * *", "first");
        await store.UpsertAsync(job);
        await store.UpsertAsync(job with { Name = "v2", Prompt = "second" });

        var all = await store.GetAllAsync();
        await Assert.That(all).HasSingleItem();
        await Assert.That(all[0].Name).IsEqualTo("v2");
        await Assert.That(all[0].Prompt).IsEqualTo("second");
    }

    [Test]
    public async Task RemoveReturnsTrueOnceAndFalseAfterward()
    {
        using var fixture = new TempRoot();
        var store = NewStore(fixture);

        var job = NewJob("agent-a", "n", "0 0 * * *", "p");
        await store.UpsertAsync(job);

        await Assert.That(await store.RemoveAsync(job.Id)).IsTrue();
        await Assert.That(await store.RemoveAsync(job.Id)).IsFalse();
        await Assert.That(await store.GetAsync(job.Id)).IsNull();
    }

    [Test]
    public async Task LoadHandlesGarbageJsonByStartingEmpty()
    {
        using var fixture = new TempRoot();
        var dataRoot = new ShearsPaths(Options.Create(new ShearsPathsOptions { DataRoot = fixture.Path }))
            .GetPath(PathKind.Data, ensureExists: true);
        await File.WriteAllTextAsync(Path.Combine(dataRoot, "cron.json"), "{ this is not json");

        var store = NewStore(fixture);

        var jobs = await store.GetAllAsync();
        await Assert.That(jobs).IsEmpty();
    }

    [Test]
    public async Task LoadDedupesDuplicateIdsLastWriteWins()
    {
        using var fixture = new TempRoot();
        var dataRoot = new ShearsPaths(Options.Create(new ShearsPathsOptions { DataRoot = fixture.Path }))
            .GetPath(PathKind.Data, ensureExists: true);
        var sharedId = Guid.NewGuid();
        var earlier = NewJob("agent-a", "earlier", "0 0 * * *", "earlier-prompt") with { Id = sharedId };
        var later = NewJob("agent-a", "later", "0 1 * * *", "later-prompt") with { Id = sharedId };
        await File.WriteAllTextAsync(
            Path.Combine(dataRoot, "cron.json"),
            JsonSerializer.Serialize(new[] { earlier, later }));

        var store = NewStore(fixture);

        var jobs = await store.GetAllAsync();
        await Assert.That(jobs).HasSingleItem();
        await Assert.That(jobs[0].Name).IsEqualTo("later");
        await Assert.That(jobs[0].Prompt).IsEqualTo("later-prompt");
    }

    private static ICronStore NewStore(TempRoot fixture)
    {
        IShearsPaths paths = new ShearsPaths(Options.Create(new ShearsPathsOptions { DataRoot = fixture.Path }));
        return new JsonCronStore(paths, NullLogger<JsonCronStore>.Instance);
    }

    private static CronJob NewJob(string agentId, string name, string expression, string prompt) =>
        new CronJob(Id: Guid.NewGuid(), AgentId: agentId, Name: name, CronExpression: expression, Prompt: prompt,
            CreatedAt: DateTimeOffset.UnixEpoch);

    private sealed class TempRoot : IDisposable
    {
        public TempRoot()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"llamashears-cron-{Guid.NewGuid():N}");
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
