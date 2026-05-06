# Agent heartbeat

## What it is

A heartbeat is the agent's mechanism to act on its own when nothing else is asking it to. It is a single, well-defined kind of input — a `User`-roled turn (with `FrameworkUser` provenance, since the framework authored it on the user's behalf) — that prompts the model and lets the agent decide whether to do something or do nothing. The model is in charge of the answer; the framework's job is only to fire the heartbeat at the right times.

A heartbeat is not the system tick. The system tick is a periodic housekeeping signal — its sole job is to nudge agents to check whether their heartbeat is due. Multiple ticks may fire between heartbeats. A heartbeat may also be skipped on any given interval (see *Disabled vs. silent*).

## When it fires

Each agent has a heartbeat **period** (a `TimeSpan`). On every system tick, the agent compares `now - LastHeartbeatAt` against the period. If the elapsed time is at or past the period, the agent reads its heartbeat file *and resets `LastHeartbeatAt` to now*. If the file has content, that content becomes the heartbeat turn (see *What the prompt is*). If the file is missing or empty at that moment, no heartbeat fires for this interval — but the timer still resets, so the next opportunity is one full period later.

This is deliberate: the period is a throttle, not just a delay. A heartbeat fires *at most* every period, regardless of file state. An agent that wants faster wake-up granularity must shorten its period.

Concretely, with a 30-minute period:

- Tick at elapsed = 29m 58s → period not elapsed; nothing happens.
- 30 seconds later, next tick at elapsed = 30m 28s → period elapsed; file is read; `LastHeartbeatAt` resets to now. If the file is non-empty, heartbeat fires. If empty/missing, no heartbeat — but the next opportunity is still ~30 minutes from this tick.
- File appears 90 seconds after that reset → not picked up immediately; next pickup is ~30 minutes from the reset.

The tick interval bounds the *granularity* of heartbeat firing, not its average rate. A heartbeat configured for "every 30 minutes" will, in practice, fire every "30 minutes plus up to one tick interval" — and only if the file has content at the moment of the check. That is an accepted trade for not running a per-agent timer.

## What the prompt is

The heartbeat prompt is the contents of `HEARTBEAT.md` in the agent's workspace (see [agent-workspace.md](agent-workspace.md)). The workspace's on-disk location is still **TBD**; what matters here is the file's *existence and contents* as the contract:

- The file's text is the body of the user-typed turn delivered to the model.
- The framework prepends some system information to the prompt (current time, last heartbeat time, agent id, and any other context the framework deems relevant for the model to reason about *why it was just woken*). The exact fields are **TBD**.
- The combined turn is fed to the agent's input pipeline as a `FrameworkUser` turn — it queues like any other input. From the model's perspective there is nothing special about a heartbeat; it is a user message, just one the human did not type.

The model's response is fanned out to the agent's output channels exactly as it would be for a human-authored input. The model may choose to "do nothing" simply by responding with a no-op or by using whatever the agent's tool surface supports for "no action."

## Disabled vs. silent: two different states

These are not the same thing, and the framework treats them differently.

### Disabled (`HeartbeatPeriod <= 0` in the agent config)

A deliberate, static decision in the agent's JSON config. The framework records this **once at agent load** with a log line, and from that point on never reads the heartbeat file for this agent. Heartbeats are off until the config changes (which triggers a reload anyway). This is the only true "disabled" state.

### Silent (period > 0, but the heartbeat file is missing or empty)

When `HeartbeatPeriod > 0`, the heartbeat is *enabled*; whether it fires on any given interval depends on the file's state at the moment the period elapses. Missing/empty file at that moment → no heartbeat fires this interval. The timer still resets, so the next opportunity is one full period later. This is a normal runtime state, not a configuration error, and is **not** logged per occurrence.

The heartbeat file is **expected to change at runtime**. The framework treats it as a live signal, not a configuration artifact. Agents may use it as a self-controlled wake-up mechanism: write content into it ahead of an upcoming check, let the framework deliver that content on the next eligible tick, and then delete or empty the file as part of processing the heartbeat to "consume" the request — recreating it later when the agent next wants to be woken. The framework's only contract is *"if the file has content when the period has elapsed, deliver it (once)."* The throttle still applies: writing content right after a missed check means waiting another full period for it to be seen. What an agent does with the file beyond that is entirely the agent's prerogative.

There is intentionally no `enabled: true/false` field in the agent config. The two existing knobs (period value, file state) cover the two distinct intents: the period is for static, agent-author choices; the file is for dynamic, agent-runtime choices. Adding a third boolean would force a "which one wins" reconciliation with no satisfying answer.

## Where this fits in the agent's processing model

Heartbeats are one source of input among several (the others being `IInputChannel` instances). All inputs flow through the same queue. When the agent's processor is busy, queued heartbeats wait their turn alongside any other queued turns and are delivered together on the next cycle.

This means a heartbeat is **not** guaranteed to result in a discrete model call by itself — if other inputs are already waiting, the heartbeat turn rides along in the same prompt. That is the correct behavior: a heartbeat is "wake up and consider what's happening," and "what's happening" includes any pending inputs.

The agent's processing model itself (queue semantics, when work runs, what counts as "busy") is a separate concern from the heartbeat. The heartbeat documented here only specifies *when a heartbeat turn enters the queue and what it contains*.

## Open items

- Heartbeat prompt file location — `HEARTBEAT.md` in the agent's workspace; the workspace location itself is still pending (see [agent-workspace.md](agent-workspace.md)).
- Exact "system information" prepended to the heartbeat prompt — pending; minimum viable set is current UTC, last heartbeat UTC, agent id.
- Whether heartbeats persist (counter, last-fired timestamp) across host restarts, or always start fresh from the agent's load time. Currently no persistence is wired; this defaults to "fresh" by absence.
