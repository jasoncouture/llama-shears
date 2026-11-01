using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Timer;

public sealed partial class TimerScheduler : ITimerScheduler, IHostedService, IDisposable
{
    public const int MinSeconds = 1;
    public const int MaxSeconds = 7 * 24 * 60 * 60;
    public const int MaxPromptCharacters = 8 * 1024;
    public const int MaxTimersPerSession = 32;
    public const string WakeupChannelId = "timer";

    private readonly IEventBus _bus;
    private readonly TimeProvider _time;
    private readonly ILogger<TimerScheduler> _logger;
    private readonly Lock _gate = new Lock();
    private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
    private readonly Dictionary<Guid, PendingSessionTimer> _pending = [];
    private readonly List<ITimer> _expired = [];
    private bool _disposed;

    public TimerScheduler(IEventBus bus, TimeProvider time, ILogger<TimerScheduler> logger)
    {
        _bus = bus;
        _time = time;
        _logger = logger;
    }

    public TimerSetResult Schedule(SessionId session, int seconds, string prompt)
    {
        ArgumentNullException.ThrowIfNull(session);
        DrainExpired();

        if (string.IsNullOrWhiteSpace(prompt))
        {
            return Refuse("Refused: prompt is required.");
        }

        if (prompt.Length > MaxPromptCharacters)
        {
            return Refuse($"Refused: prompt must be at most {MaxPromptCharacters} characters.");
        }

        if (seconds < MinSeconds || seconds > MaxSeconds)
        {
            return Refuse($"Refused: seconds must be between {MinSeconds} and {MaxSeconds}.");
        }

        var firesAt = _time.GetUtcNow().AddSeconds(seconds);
        var id = Guid.CreateVersion7();
        var pending = new PendingSessionTimer(session, prompt, firesAt);

        lock (_gate)
        {
            if (_disposed)
            {
                return Refuse("Refused: timer scheduler is stopped.");
            }

            if (CountFor(session) >= MaxTimersPerSession)
            {
                return Refuse($"Refused: this session already has {MaxTimersPerSession} timers set.");
            }

            pending.Timer = _time.CreateTimer(
                OnFired,
                id,
                TimeSpan.FromSeconds(seconds),
                Timeout.InfiniteTimeSpan);
            _pending.Add(id, pending);
        }

        LogArmed(id, session.ToString(), seconds);
        return new TimerSetResult(
            Set: true,
            TimerId: id,
            SessionId: session.ToString(),
            Seconds: seconds,
            FiresAt: firesAt,
            Prompt: prompt);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        PendingSessionTimer[] pending;
        ITimer[] expired;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            pending = [.. _pending.Values];
            expired = [.. _expired];
            _pending.Clear();
            _expired.Clear();
        }

        _shutdown.Cancel();
        foreach (var item in pending)
        {
            item.Timer?.Dispose();
        }

        foreach (var timer in expired)
        {
            timer.Dispose();
        }
    }

    private void OnFired(object? state)
    {
        if (state is not Guid id)
        {
            return;
        }

        PendingSessionTimer? pending;
        lock (_gate)
        {
            if (!_pending.Remove(id, out pending))
            {
                return;
            }

            if (pending.Timer is not null)
            {
                // Dispose from Schedule or shutdown, not this callback.
                // Timer.Dispose on the callback thread can deadlock.
                _expired.Add(pending.Timer);
            }
        }

        _ = DeliverAsync(id, pending);
    }

    private async Task DeliverAsync(Guid id, PendingSessionTimer pending)
    {
        try
        {
            var cancellationToken = _shutdown.Token;
            cancellationToken.ThrowIfCancellationRequested();
            var message = new ChannelMessage(FormatWakeup(pending.Prompt), WakeupChannelId, pending.FiresAt);
            await _bus.PublishAsync(
                Event.WellKnown.Channel.Message with { Id = pending.Session },
                message,
                cancellationToken).ConfigureAwait(false);
            LogFired(id, pending.Session.ToString());
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            LogDeliveryFailed(id, pending.Session.ToString(), ex);
        }
    }

    private void DrainExpired()
    {
        ITimer[] expired;
        lock (_gate)
        {
            if (_expired.Count == 0)
            {
                return;
            }

            expired = [.. _expired];
            _expired.Clear();
        }

        foreach (var timer in expired)
        {
            timer.Dispose();
        }
    }

    private int CountFor(SessionId session)
    {
        var count = 0;
        foreach (var pending in _pending.Values)
        {
            if (pending.Session.Id == session.Id)
            {
                count++;
            }
        }

        return count;
    }

    private static TimerSetResult Refuse(string error) => new TimerSetResult(Set: false, Error: error);

    private static string FormatWakeup(string prompt) => $"Timer wakeup.\n\n{prompt}";

    [LoggerMessage(Level = LogLevel.Information, Message = "Armed timer '{TimerId}' for session '{SessionId}' ({Seconds}s).")]
    private partial void LogArmed(Guid timerId, string sessionId, int seconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Timer '{TimerId}' woke session '{SessionId}'.")]
    private partial void LogFired(Guid timerId, string sessionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Timer '{TimerId}' failed to wake session '{SessionId}'.")]
    private partial void LogDeliveryFailed(Guid timerId, string sessionId, Exception exception);
}
