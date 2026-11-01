---
name: timer_set wakes the calling session
description: Bundled MCP timer_set arms an in-memory one-shot timer and later injects a timer-wakeup turn into the calling session
type: project
---

`timer_set` (`LlamaShears.Api.Tools.ModelContextProtocol.Timer`) arms a one-shot timer on the calling session and returns immediately. The tool call does not sleep.

- `seconds` is an integer from 1 to 604800 (7 days). `prompt` is required and at most 8192 characters. At most 32 timers may be pending on one session.
- When the delay elapses, the scheduler publishes `Event.WellKnown.Channel.Message` to the calling session. `ChannelId` is `timer`. The text is `Timer wakeup.` then a blank line, then `prompt` verbatim. `ChannelMessageIntakeService` stores that as a user-role turn.
- Write `prompt` as a directive to the future self, the same way a cron prompt is written. The wakeup always targets the caller, not a chosen session id.
- Timers live in a process-wide `TimerScheduler` (`IHostedService`). They are not written to disk. Shutdown cancels anything still pending, and a restart does not fire them. `cron_schedule` is the durable schedule.
- The tool's `CancellationToken` only refuses an already-cancelled call. It does not own the armed timer.
