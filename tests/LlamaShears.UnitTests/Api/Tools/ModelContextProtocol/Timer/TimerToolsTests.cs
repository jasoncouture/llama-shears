using LlamaShears.Api.Tools.ModelContextProtocol.Timer;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.UnitTests.Agent.Pipeline;
using NSubstitute;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Timer;

public sealed class TimerToolsTests
{
    [Test]
    public async Task MissingAgentIsRefused()
    {
        var scope = PipelineTestContext.ScopeFor();
        scope.SetItem(AgentConfig.DataKey, null);
        var scheduler = Substitute.For<ITimerScheduler>();
        var tools = new TimerTools(scope, scheduler);

        var result = tools.SetTimer(5, "wake");

        await Assert.That(result.Set).IsFalse();
        await Assert.That(result.Error).IsEqualTo("Refused: timer_set requires an authenticated agent on the request.");
        scheduler.DidNotReceive().Schedule(Arg.Any<SessionId>(), Arg.Any<int>(), Arg.Any<string>());
    }

    [Test]
    public async Task SetTimerSchedulesTheCallingSession()
    {
        var scope = PipelineTestContext.ScopeFor();
        var session = scope.GetCurrentSessionId();
        var scheduled = new TimerSetResult(
            Set: true,
            TimerId: Guid.CreateVersion7(),
            SessionId: session.ToString(),
            Seconds: 5,
            FiresAt: DateTimeOffset.UnixEpoch,
            Prompt: "wake");
        var scheduler = Substitute.For<ITimerScheduler>();
        scheduler.Schedule(Arg.Any<SessionId>(), Arg.Any<int>(), Arg.Any<string>()).Returns(scheduled);
        var tools = new TimerTools(scope, scheduler);

        var result = tools.SetTimer(5, "wake");

        await Assert.That(result).IsEqualTo(scheduled);
        scheduler.Received(1).Schedule(
            Arg.Is<SessionId>(candidate => candidate.ToString() == session.ToString()),
            5,
            "wake");
    }

    [Test]
    public async Task CancelledCallDoesNotSchedule()
    {
        var scope = PipelineTestContext.ScopeFor();
        var scheduler = Substitute.For<ITimerScheduler>();
        var tools = new TimerTools(scope, scheduler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.That(() => tools.SetTimer(5, "wake", cancellation.Token))
            .Throws<OperationCanceledException>();
        scheduler.DidNotReceive().Schedule(Arg.Any<SessionId>(), Arg.Any<int>(), Arg.Any<string>());
    }
}
