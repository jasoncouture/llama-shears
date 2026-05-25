using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Cron;
using LlamaShears.Core.Paths;
using LlamaShears.UnitTests.Agent.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace LlamaShears.UnitTests.Cron;

public sealed class CronSchedulerTests
{
    [Test]
    public async Task ScheduleComputesNextFireFromExpressionAndCurrentTime()
    {
        using var fixture = new TempRoot();
        var time = NewTime(new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero));
        var scheduler = NewScheduler(fixture, "agent-a", time);

        var job = await scheduler.ScheduleAsync("hourly-ish", "0 11 * * *", "wake up");

        await Assert.That(job.NextFireAt).IsEqualTo(new DateTimeOffset(2026, 5, 7, 11, 0, 0, TimeSpan.Zero));
        await Assert.That(job.AgentId).IsEqualTo("agent-a");
        await Assert.That(job.Enabled).IsTrue();
        await Assert.That(job.LastFiredAt).IsNull();
    }

    [Test]
    public async Task ScheduleRejectsUnparseableExpression()
    {
        using var fixture = new TempRoot();
        var scheduler = NewScheduler(fixture, "agent-a");

        await Assert.That(async () => await scheduler.ScheduleAsync("bad", "this is not cron", "p"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task ListReturnsOnlyTheCallingAgentsJobs()
    {
        using var fixture = new TempRoot();
        var schedulerA = NewScheduler(fixture, "agent-a");
        var schedulerB = NewScheduler(fixture, "agent-b");

        await schedulerA.ScheduleAsync("a1", "0 0 * * *", "p");
        await schedulerA.ScheduleAsync("a2", "0 1 * * *", "p");
        await schedulerB.ScheduleAsync("b1", "0 2 * * *", "p");

        var aJobs = await schedulerA.ListAsync();
        var bJobs = await schedulerB.ListAsync();

        await Assert.That(aJobs.Count).IsEqualTo(2);
        await Assert.That(bJobs.Count).IsEqualTo(1);
        await Assert.That(bJobs[0].Name).IsEqualTo("b1");
    }

    [Test]
    public async Task CancelRefusesOtherAgentsJob()
    {
        using var fixture = new TempRoot();
        var schedulerA = NewScheduler(fixture, "agent-a");
        var schedulerB = NewScheduler(fixture, "agent-b");

        var bJob = await schedulerB.ScheduleAsync("b1", "0 0 * * *", "p");

        await Assert.That(await schedulerA.CancelAsync(bJob.Id)).IsFalse();

        var bAfter = await schedulerB.ListAsync();
        await Assert.That(bAfter).HasSingleItem();
    }

    [Test]
    public async Task EditPatchesOnlyProvidedFields()
    {
        using var fixture = new TempRoot();
        var scheduler = NewScheduler(fixture, "agent-a");

        var job = await scheduler.ScheduleAsync("name1", "0 0 * * *", "prompt1");
        var edited = await scheduler.EditAsync(job.Id, new CronJobEdit(Name: "name2"));

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
        var scheduler = NewScheduler(fixture, "agent-a");
        var job = await scheduler.ScheduleAsync("n", "0 0 * * *", "p");

        await Assert.That(async () => await scheduler.EditAsync(job.Id, new CronJobEdit(Name: blank)))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task EditRejectsBlankPrompt()
    {
        using var fixture = new TempRoot();
        var scheduler = NewScheduler(fixture, "agent-a");
        var job = await scheduler.ScheduleAsync("n", "0 0 * * *", "p");

        await Assert.That(async () => await scheduler.EditAsync(job.Id, new CronJobEdit(Prompt: "   ")))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task EditRejectsBlankCronExpression()
    {
        using var fixture = new TempRoot();
        var scheduler = NewScheduler(fixture, "agent-a");
        var job = await scheduler.ScheduleAsync("n", "0 0 * * *", "p");

        await Assert.That(async () => await scheduler.EditAsync(job.Id, new CronJobEdit(CronExpression: "  ")))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task EditRecomputesNextFireWhenExpressionChanges()
    {
        using var fixture = new TempRoot();
        var time = NewTime(new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero));
        var scheduler = NewScheduler(fixture, "agent-a", time);

        var job = await scheduler.ScheduleAsync("n", "0 11 * * *", "p");
        var edited = await scheduler.EditAsync(job.Id, new CronJobEdit(CronExpression: "0 12 * * *"));

        await Assert.That(edited!.NextFireAt).IsEqualTo(new DateTimeOffset(2026, 5, 7, 12, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public async Task TriggerUpdatesLastFiredAndAdvancesNext()
    {
        using var fixture = new TempRoot();
        var time = NewTime(new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero));
        var scheduler = NewScheduler(fixture, "agent-a", time);

        var job = await scheduler.ScheduleAsync("n", "0 11 * * *", "p");

        await Assert.That(await scheduler.TriggerAsync(job.Id)).IsTrue();

        var listed = await scheduler.ListAsync();
        await Assert.That(listed[0].LastFiredAt).IsEqualTo(new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero));
        await Assert.That(listed[0].NextFireAt).IsEqualTo(new DateTimeOffset(2026, 5, 7, 11, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public async Task FireDueSkipsDisabledAndJobsBeforeTheirNextFire()
    {
        using var fixture = new TempRoot();
        var time = NewTime(new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero));
        var scheduler = NewScheduler(fixture, "agent-a", time);

        var due = await scheduler.ScheduleAsync("due", "*/5 * * * *", "p");
        var notYet = await scheduler.ScheduleAsync("later", "0 23 * * *", "p");
        var disabled = await scheduler.ScheduleAsync("off", "*/5 * * * *", "p");
        await scheduler.EditAsync(disabled.Id, new CronJobEdit(Enabled: false));

        time.Advance(TimeSpan.FromMinutes(15));
        var fireAt = time.GetUtcNow();
        await scheduler.FireDueAsync(fireAt);

        var listed = (await scheduler.ListAsync()).ToDictionary(j => j.Id);

        await Assert.That(listed[due.Id].LastFiredAt).IsEqualTo(fireAt);
        await Assert.That(listed[notYet.Id].LastFiredAt).IsNull();
        await Assert.That(listed[disabled.Id].LastFiredAt).IsNull();
    }

    private static FakeTimeProvider NewTime(DateTimeOffset start) => new FakeTimeProvider(start);

    private static ICronScheduler NewScheduler(TempRoot fixture, string agentId, FakeTimeProvider? time = null)
    {
        IApplicationPathProvider paths = new ApplicationPathProvider(Options.Create(new ShearsPathsOptions { DataRoot = fixture.Path }));
        ICronStore store = new JsonCronStore(paths, NullLogger<JsonCronStore>.Instance);
        IDataContextScope scope = new FakeDataContextScope(new SessionId(agentId, SessionId.DefaultSessionName));
        scope.SetItem(AgentConfig.DataKey, NewAgentConfig(agentId));
        return new CronScheduler(store, scope, time ?? new FakeTimeProvider(DateTimeOffset.UnixEpoch), NullLogger<CronScheduler>.Instance);
    }

    private static AgentConfig NewAgentConfig(string agentId) =>
        new AgentConfig(Model: new ModelConfiguration(new CompositeIdentity("test", "model")), Id: agentId);

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
