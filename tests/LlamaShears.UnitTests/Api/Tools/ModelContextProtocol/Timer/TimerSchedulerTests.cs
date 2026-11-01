using LlamaShears.Api.Tools.ModelContextProtocol.Timer;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Timer;

public sealed class TimerSchedulerTests
{
    private static readonly DateTimeOffset _start = new DateTimeOffset(2026, 11, 1, 14, 41, 0, TimeSpan.Zero);

    [Test]
    public async Task ScheduleReturnsImmediatelyAndWakesSessionWhenDelayElapses()
    {
        using var fixture = new Fixture();
        var session = Session();
        const string prompt = "Tell the user to stand up.\nKeep it short.";

        var result = fixture.Scheduler.Schedule(session, 5, prompt);

        await Assert.That(result.Set).IsTrue();
        await Assert.That(result.Error).IsNull();
        await Assert.That(result.TimerId).IsNotNull();
        await Assert.That(result.TimerId!.Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(result.SessionId).IsEqualTo(session.ToString());
        await Assert.That(result.Seconds).IsEqualTo(5);
        await Assert.That(result.FiresAt).IsEqualTo(_start.AddSeconds(5));
        await Assert.That(result.Prompt).IsEqualTo(prompt);
        await fixture.Bus.DidNotReceiveWithAnyArgs().PublishAsync(
            default!, default(ChannelMessage), default, default);

        fixture.Time.Advance(TimeSpan.FromSeconds(4));
        await fixture.Bus.DidNotReceiveWithAnyArgs().PublishAsync(
            default!, default(ChannelMessage), default, default);

        fixture.Time.Advance(TimeSpan.FromSeconds(1));

        await fixture.Bus.Received(1).PublishAsync(
            Arg.Is<EventType>(type => type.Component == Event.WellKnown.Channel.Message.Component
                && type.EventName == Event.WellKnown.Channel.Message.EventName
                && type.Id == session.ToString()),
            Arg.Is<ChannelMessage>(message => message.ChannelId == "timer"
                && message.Text == $"Timer wakeup.\n\n{prompt}"
                && message.Timestamp == _start.AddSeconds(5)),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());

        fixture.Time.Advance(TimeSpan.FromHours(1));
        await fixture.Bus.Received(1).PublishAsync(
            Arg.Any<EventType>(),
            Arg.Any<ChannelMessage>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task WakeupPreservesPromptVerbatim()
    {
        using var fixture = new Fixture();
        var session = Session();
        const string prompt = "  起床 \"now\"  ";

        fixture.Scheduler.Schedule(session, 1, prompt);
        fixture.Time.Advance(TimeSpan.FromSeconds(1));

        await fixture.Bus.Received(1).PublishAsync(
            Arg.Any<EventType>(),
            Arg.Is<ChannelMessage>(message => message.Text == $"Timer wakeup.\n\n{prompt}"),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TimersWakeOnlyTheirOwnSession()
    {
        using var fixture = new Fixture();
        var first = Session("default");
        var second = Session("other");

        fixture.Scheduler.Schedule(first, 2, "first prompt");
        fixture.Scheduler.Schedule(second, 5, "second prompt");

        fixture.Time.Advance(TimeSpan.FromSeconds(2));
        await fixture.Bus.Received(1).PublishAsync(
            Arg.Is<EventType>(type => type.Id == first.ToString()),
            Arg.Is<ChannelMessage>(message => message.Text == "Timer wakeup.\n\nfirst prompt"),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await fixture.Bus.DidNotReceive().PublishAsync(
            Arg.Is<EventType>(type => type.Id == second.ToString()),
            Arg.Any<ChannelMessage>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());

        fixture.Time.Advance(TimeSpan.FromSeconds(3));
        await fixture.Bus.Received(1).PublishAsync(
            Arg.Is<EventType>(type => type.Id == second.ToString()),
            Arg.Is<ChannelMessage>(message => message.Text == "Timer wakeup.\n\nsecond prompt"),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    [Arguments(TimerScheduler.MaxSeconds + 1)]
    public async Task ScheduleRejectsSecondsOutOfRange(int seconds)
    {
        using var fixture = new Fixture();

        var result = fixture.Scheduler.Schedule(Session(), seconds, "wake");

        await Assert.That(result.Set).IsFalse();
        await Assert.That(result.Error).IsEqualTo(
            $"Refused: seconds must be between {TimerScheduler.MinSeconds} and {TimerScheduler.MaxSeconds}.");
        await fixture.Bus.DidNotReceiveWithAnyArgs().PublishAsync(
            default!, default(ChannelMessage), default, default);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments("\n\t")]
    public async Task ScheduleRejectsBlankPrompt(string? prompt)
    {
        using var fixture = new Fixture();

        var result = fixture.Scheduler.Schedule(Session(), 5, prompt!);

        await Assert.That(result.Set).IsFalse();
        await Assert.That(result.Error).IsEqualTo("Refused: prompt is required.");
        await Assert.That(result.Prompt).IsNull();
    }

    [Test]
    public async Task ScheduleRejectsPromptOverTheCharacterCap()
    {
        using var fixture = new Fixture();
        var prompt = new string('a', TimerScheduler.MaxPromptCharacters + 1);

        var result = fixture.Scheduler.Schedule(Session(), 5, prompt);

        await Assert.That(result.Set).IsFalse();
        await Assert.That(result.Error).IsEqualTo(
            $"Refused: prompt must be at most {TimerScheduler.MaxPromptCharacters} characters.");
    }

    [Test]
    public async Task ScheduleAcceptsPromptAtTheCharacterCap()
    {
        using var fixture = new Fixture();
        var prompt = new string('b', TimerScheduler.MaxPromptCharacters);

        var result = fixture.Scheduler.Schedule(Session(), 1, prompt);

        await Assert.That(result.Set).IsTrue();
        await Assert.That(result.Prompt).IsEqualTo(prompt);
    }

    [Test]
    public async Task ScheduleRejectsNullSession()
    {
        using var fixture = new Fixture();

        await Assert.That(() => fixture.Scheduler.Schedule(null!, 5, "wake"))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ScheduleCapsPendingTimersPerSession()
    {
        using var fixture = new Fixture();
        var session = Session();
        var other = Session("other");

        for (var i = 0; i < TimerScheduler.MaxTimersPerSession; i++)
        {
            var armed = fixture.Scheduler.Schedule(session, 10, $"prompt {i}");
            await Assert.That(armed.Set).IsTrue();
        }

        var refused = fixture.Scheduler.Schedule(session, 10, "one more");
        await Assert.That(refused.Set).IsFalse();
        await Assert.That(refused.Error).IsEqualTo(
            $"Refused: this session already has {TimerScheduler.MaxTimersPerSession} timers set.");

        var otherArmed = fixture.Scheduler.Schedule(other, 10, "other session");
        await Assert.That(otherArmed.Set).IsTrue();
    }

    [Test]
    public async Task StopAsyncDropsPendingTimersAndRefusesNewOnes()
    {
        using var fixture = new Fixture();
        var session = Session();
        fixture.Scheduler.Schedule(session, 5, "wake");

        await fixture.Scheduler.StopAsync(CancellationToken.None);
        fixture.Time.Advance(TimeSpan.FromSeconds(5));

        await fixture.Bus.DidNotReceiveWithAnyArgs().PublishAsync(
            default!, default(ChannelMessage), default, default);
        var refused = fixture.Scheduler.Schedule(session, 5, "again");
        await Assert.That(refused.Set).IsFalse();
        await Assert.That(refused.Error).IsEqualTo("Refused: timer scheduler is stopped.");

        await fixture.Scheduler.StopAsync(CancellationToken.None);
    }

    private static SessionId Session(string name = SessionId.DefaultSessionName)
        => new SessionId("alice", name);

    private sealed class Fixture : IDisposable
    {
        public Fixture()
        {
            Time = new FakeTimeProvider(_start);
            Bus = Substitute.For<IEventBus>();
            Scheduler = new TimerScheduler(Bus, Time, NullLogger<TimerScheduler>.Instance);
        }

        public FakeTimeProvider Time { get; }

        public IEventBus Bus { get; }

        public TimerScheduler Scheduler { get; }

        public void Dispose() => Scheduler.Dispose();
    }
}
