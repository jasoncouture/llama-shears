# Agent heartbeat

## What it is

A heartbeat is the agent's mechanism to act on its own when nothing else is asking it to. It is a single, well-defined kind of input — a `User`-roled turn (with `FrameworkUser` provenance, since the framework authored it on the user's behalf) — that prompts the model and lets the agent decide whether to do something or do nothing. The model is in charge of the answer; the framework's job is only to fire the heartbeat at the right times.

A heartbeat is not the system tick. The system tick is a periodic housekeeping signal — its sole job is to nudge agents to check whether their heartbeat is due. Multiple ticks may fire between heartbeats. A heartbeat may also be skipped entirely (see *Disabling*).

## When it fires

Each agent has a heartbeat **period** (a `TimeSpan`). On every system tick, the agent compares `now - LastHeartbeatAt` against the period. If the elapsed time is at or past the period, the agent fires its heartbeat and resets `LastHeartbeatAt`.

Concretely, with a 30-minute period:

- Tick at elapsed = 29m 58s → no heartbeat (under threshold).
- 30 seconds later, next tick at elapsed = 30m 28s → heartbeat fires; elapsed resets to 0.

The tick interval bounds the *granularity* of heartbeat firing, not its average rate. A heartbeat configured for "every 30 minutes" will, in practice, fire every "30 minutes plus up to one tick interval." That is an accepted trade for not running a per-agent timer.

## What the prompt is

The heartbeat prompt is the contents of a file on disk — specifically, a heartbeat prompt file in the agent's workspace. The exact path is **TBD** and will be specified in a follow-up edit; for now treat the file's *existence and contents* as the contract:

- The file's text is the body of the user-typed turn delivered to the model.
- The framework prepends some system information to the prompt (current time, last heartbeat time, agent id, and any other context the framework deems relevant for the model to reason about *why it was just woken*). The exact fields are **TBD**.
- The combined turn is fed to the agent's input pipeline as a `FrameworkUser` turn — it queues like any other input. From the model's perspective there is nothing special about a heartbeat; it is a user message, just one the human did not type.

The model's response is fanned out to the agent's output channels exactly as it would be for a human-authored input. The model may choose to "do nothing" simply by responding with a no-op or by using whatever the agent's tool surface supports for "no action."

## Disabling

A heartbeat is disabled when **any** of the following holds:

- The heartbeat prompt file is missing.
- The heartbeat prompt file is empty (zero bytes, or whitespace-only).
- The agent's configured `HeartbeatPeriod` is less than or equal to `TimeSpan.Zero`.

There is intentionally no `enabled: true/false` field in the agent config. Both knobs (file presence, period value) are existing surfaces being repurposed; adding a third would create a "which one wins" question with no satisfying answer. The file is the canonical mechanism; the `<= 0` period is the escape hatch when you want to keep the file around but pause the heartbeat without deleting content.

A disabled heartbeat is *silent* — the agent simply never fires one. No log, no error. Disabling is a normal configuration state.

## Where this fits in the agent's processing model

Heartbeats are one source of input among several (the others being `IInputChannel` instances). All inputs flow through the same queue. When the agent's processor is busy, queued heartbeats wait their turn alongside any other queued turns and are delivered together on the next cycle.

This means a heartbeat is **not** guaranteed to result in a discrete model call by itself — if other inputs are already waiting, the heartbeat turn rides along in the same prompt. That is the correct behavior: a heartbeat is "wake up and consider what's happening," and "what's happening" includes any pending inputs.

The agent's processing model itself (queue semantics, when work runs, what counts as "busy") is a separate concern from the heartbeat. The heartbeat documented here only specifies *when a heartbeat turn enters the queue and what it contains*.

## Open items

- Heartbeat prompt file location — pending; will be wired alongside the agent workspace path.
- Exact "system information" prepended to the heartbeat prompt — pending; minimum viable set is current UTC, last heartbeat UTC, agent id.
- Whether heartbeats persist (counter, last-fired timestamp) across host restarts, or always start fresh from the agent's load time. Currently no persistence is wired; this defaults to "fresh" by absence.
