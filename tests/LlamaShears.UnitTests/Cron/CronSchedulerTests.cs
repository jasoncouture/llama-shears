using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Cron;
using LlamaShears.Core.Paths;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Cron;

public sealed class CronSchedulerTests
{
    [Test]
    public async Task ScheduleComputesNextFireFromExpressionAndCurrentTime()
    {
        using var fixture = new TempRoot();
        var time = NewTime(new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero));
        var scheduler = NewScheduler(fixture, time);

        var job = await scheduler.ScheduleAsync("agent-a", "hourly-ish", "0 11 * * *", "wake up");

        await Assert.That(job.NextFireAt).IsEqualTo(new DateTimeOffset(2026, 5, 7, 11, 0, 0, TimeSpan.Zero));
        await Assert.That(job.AgentId).IsEqualTo("agent-a");
        await Assert.That(job.Enabled).IsTrue();
        await Assert.That(job.LastFiredAt).IsNull();
    }

    [Test]
    public async Task ScheduleRejectsUnparseableExpression()
    {
        using var fixture = new TempRoot();
        var scheduler = NewScheduler(fixture);

        await Assert.That(async () => await scheduler.ScheduleAsync("agent-a", "bad", "this is not cron", "p"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task ListIsAgentScoped()
    {
        using var fixture = new TempRoot();
        var scheduler = NewScheduler(fixture);

        await scheduler.ScheduleAsync("agent-a", "a1", "0 0 * * *", "p");
        await scheduler.ScheduleAsync("agent-a", "a2", "0 1 * * *", "p");
        await scheduler.ScheduleAsync("agent-b", "b1", "0 2 * * *", "p");

        var aJobs = await scheduler.ListByAgentAsync("agent-a");
        var bJobs = await scheduler.ListByAgentAsync("agent-b");

        await Assert.That(aJobs.Count).IsEqualTo(2);
        await Assert.That(bJobs.Count).IsEqualTo(1);
        await Assert.That(bJobs[0].Name).IsEqualTo("b1");
    }

    [Test]
    public async Task CancelRefusesOtherAgentsJob()
    {
        using var fixture = new TempRoot();
        var scheduler = NewScheduler(fixture);

        var bJob = await scheduler.ScheduleAsync("agent-b", "b1", "0 0 * * *", "p");

        await Assert.That(await scheduler.CancelAsync("agent-a", bJob.Id)).IsFalse();

        var bAfter = await scheduler.ListByAgentAsync("agent-b");
        await Assert.That(bAfter).HasSingleItem();
    }

    [Test]
    public async Task EditPatchesOnlyProvidedFields()
    {
        using var fixture = new TempRoot();
        var scheduler = NewScheduler(fixture);

        var job = await scheduler.ScheduleAsync("agent-a", "name1", "0 0 * * *", "prompt1");
        var edited = await scheduler.EditAsync("agent-a", job.Id, new CronJobEdit(Name: "name2"));

        await Assert.That(edited).IsNotNull();
        await Assert.That(edited!.Name).IsEqualTo("name2");
        await Assert.That(edited.Prompt).IsEqualTo("prompt1");
        await Assert.That(edited.CronExpression).IsEqualTo("0 0 * * *");
    }

    [Test]
    [Arguments("  ")]
    [Arguments("\t")]
    public async Task EditRejectsBlankName(string blank)
    {
        using var fixture = new TempRoot();
        var scheduler = NewScheduler(fixture);
        var job = await scheduler.ScheduleAsync("agent-a", "n", "0 0 * * *", "p");

        await Assert.That(async () => await scheduler.EditAsync("agent-a", job.Id, new CronJobEdit(Name: blank)))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task EditRejectsBlankPrompt()
    {
        using var fixture = new TempRoot();
        var scheduler = NewScheduler(fixture);
        var job = await scheduler.ScheduleAsync("agent-a", "n", "0 0 * * *", "p");

        await Assert.That(async () => await scheduler.EditAsync("agent-a", job.Id, new CronJobEdit(Prompt: "   ")))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task EditRejectsBlankCronExpression()
    {
        using var fixture = new TempRoot();
        var scheduler = NewScheduler(fixture);
        var job = await scheduler.ScheduleAsync("agent-a", "n", "0 0 * * *", "p");

        await Assert.That(async () => await scheduler.EditAsync("agent-a", job.Id, new CronJobEdit(CronExpression: "  ")))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task EditRecomputesNextFireWhenExpressionChanges()
    {
        using var fixture = new TempRoot();
        var time = NewTime(new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero));
        var scheduler = NewScheduler(fixture, time);

        var job = await scheduler.ScheduleAsync("agent-a", "n", "0 11 * * *", "p");
        var edited = await scheduler.EditAsync("agent-a", job.Id, new CronJobEdit(CronExpression: "0 12 * * *"));

        await Assert.That(edited!.NextFireAt).IsEqualTo(new DateTimeOffset(2026, 5, 7, 12, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public async Task TriggerUpdatesLastFiredAndAdvancesNext()
    {
        using var fixture = new TempRoot();
        var time = NewTime(new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero));
        var scheduler = NewScheduler(fixture, time);

        var job = await scheduler.ScheduleAsync("agent-a", "n", "0 11 * * *", "p");

        await Assert.That(await scheduler.TriggerAsync("agent-a", job.Id)).IsTrue();

        var listed = await scheduler.ListByAgentAsync("agent-a");
        await Assert.That(listed[0].LastFiredAt).IsEqualTo(new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero));
        await Assert.That(listed[0].NextFireAt).IsEqualTo(new DateTimeOffset(2026, 5, 7, 11, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public async Task FireDueSkipsDisabledAndJobsBeforeTheirNextFire()
    {
        using var fixture = new TempRoot();
        var time = NewTime(new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero));
        var scheduler = NewScheduler(fixture, time);

        var due = await scheduler.ScheduleAsync("agent-a", "due", "*/5 * * * *", "p");
        var notYet = await scheduler.ScheduleAsync("agent-a", "later", "0 23 * * *", "p");
        var disabled = await scheduler.ScheduleAsync("agent-a", "off", "*/5 * * * *", "p");
        await scheduler.EditAsync("agent-a", disabled.Id, new CronJobEdit(Enabled: false));

        time.Advance(TimeSpan.FromMinutes(15));
        var fireAt = time.GetUtcNow();
        await scheduler.FireDueAsync(fireAt);

        var listed = (await scheduler.ListByAgentAsync("agent-a")).ToDictionary(j => j.Id);

        await Assert.That(listed[due.Id].LastFiredAt).IsEqualTo(fireAt);
        await Assert.That(listed[notYet.Id].LastFiredAt).IsNull();
        await Assert.That(listed[disabled.Id].LastFiredAt).IsNull();
    }

    private static FakeTimeProvider NewTime(DateTimeOffset start) => new FakeTimeProvider(start);

    private static ICronScheduler NewScheduler(TempRoot fixture, FakeTimeProvider? time = null)
    {
        IApplicationPathProvider paths = new ApplicationPathProvider(Options.Create(new ShearsPathsOptions { DataRoot = fixture.Path }));
        ICronStore store = new JsonCronStore(paths, NullLogger<JsonCronStore>.Instance);
        return new CronScheduler(store, time ?? new FakeTimeProvider(DateTimeOffset.UnixEpoch), NullLogger<CronScheduler>.Instance);
    }

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
