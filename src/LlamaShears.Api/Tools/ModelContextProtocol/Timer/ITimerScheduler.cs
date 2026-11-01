using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Timer;

/// <summary>
/// Arms one-shot in-memory timers that later wake a session.
/// </summary>
public interface ITimerScheduler
{
    /// <summary>
    /// Arms a timer for <paramref name="session"/>. Returns immediately; the wakeup is a later channel message.
    /// </summary>
    TimerSetResult Schedule(SessionId session, int seconds, string prompt);
}
