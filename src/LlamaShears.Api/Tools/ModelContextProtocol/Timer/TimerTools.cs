using System.ComponentModel;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Timer;

[McpServerToolType]
public sealed class TimerTools
{
    private readonly IDataContextScope _dataScope;
    private readonly ITimerScheduler _scheduler;

    public TimerTools(IDataContextScope dataScope, ITimerScheduler scheduler)
    {
        _dataScope = dataScope;
        _scheduler = scheduler;
    }

    [McpServerTool(Name = "timer_set", Destructive = false, OpenWorld = false)]
    [Description("Arms a one-shot timer on this session and returns immediately. Does not block or sleep the turn. After seconds elapse, this same session receives a new user-role turn whose text is \"Timer wakeup.\" followed by a blank line and prompt verbatim. Write prompt as a directive to your future self — what to do when the timer fires — not as the literal words you want spoken. seconds is an integer from 1 to 604800 (7 days). At most 32 timers may be pending on this session, and prompt must be at most 8192 characters. The timer lives in host memory only: it is not saved, and it will not fire if the host restarts. Use cron when the schedule must survive a restart. Returns a JSON object with set, timerId, sessionId, seconds, firesAt, prompt, and error.")]
    public TimerSetResult SetTimer(
        [Description("How long to wait before waking this session, in seconds. Integer from 1 to 604800 (7 days).")]
        int seconds,
        [Description("Instruction delivered verbatim after the timer-wakeup marker. Write it as a directive to your future self.")]
        string prompt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_dataScope.TryGetAgentConfig() is null)
        {
            return new TimerSetResult(
                Set: false,
                Error: "Refused: timer_set requires an authenticated agent on the request.");
        }

        var session = _dataScope.GetCurrentSessionId();
        return _scheduler.Schedule(session, seconds, prompt);
    }
}
