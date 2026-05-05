# System tick

The system tick is the host's only periodic signal. Subsystems that need to do something on a cadence subscribe to it instead of running their own timers.

## Implementation

[`SystemTickService`](../../src/LlamaShears.Core/SystemTickService.cs) is a `BackgroundService` registered by `CoreServiceCollectionExtensions.AddCore`. It runs a `PeriodicTimer` at a fixed `30s` interval and publishes:

- **Event:** `system:tick`
- **Payload:** [`SystemTick(Time)`](../../src/LlamaShears.Core.Abstractions/Agent/SystemTick.cs) — `DateTimeOffset.UtcNow` at publish time.
- **Mode:** awaited (default for `IEventPublisher.PublishAsync`); subscribers can choose otherwise.

The interval is **`30s`**, hard-coded. Tuning to a different cadence is a code change. `SystemTickOptions.Enabled` (configuration section `Frame:Enabled`, default `true`) lets the operator toggle the entire tick — the timer keeps running but skips the publish — without recompiling.

The service catches all publish exceptions and logs them. A failing publish doesn't take the service down; the next tick fires regardless.

## What today's subscribers do with it

| Subscriber | What happens on tick |
|------------|----------------------|
| [`AgentManager`](../../src/LlamaShears.Core/AgentManager.cs) | Reconcile loaded agents against `<Data>/agents/*.json`. Start, reload, or stop as needed. Subscribed at `EventDeliveryMode.FireAndForget`. |

That's the whole list right now. The reconciliation work is gated by an `Interlocked` flag so a slow filesystem can't pile reconcilers on top of one another — if a previous tick's reconcile is still running when the next tick fires, the new tick is dropped. The first reconcile is also kicked off explicitly from the application-started callback so an agent doesn't have to wait up to 30s for its initial bring-up.

## What the tick is *for*

The tick exists so the host has a single, well-known cadence that any future subsystem can hang work off without growing its own timer machinery. Subsystems that *currently* run their own `PeriodicTimer` or `Task.Delay` loops do so because the work is structurally separate from reconciliation — running them off the system tick would mix concerns:

| Subsystem | Why it has its own timer |
|-----------|--------------------------|
| [`EagerCompactor`](../../src/LlamaShears.Core/EagerCompactor.cs) | Scans every minute for agents that have been idle for 15 minutes. The relevant cadence is "minutes," and the work is per-agent state, not host-level reconciliation. See [compaction.md](compaction.md). |
| [`MemoryIndexerBackgroundService`](../../src/LlamaShears.Core/Memory/MemoryIndexerBackgroundService.cs) | Walks every agent's `memory/` tree on the configured interval (default 30 min) and reconciles against the SQLite index. Distinct cadence, distinct concern. See [memory.md](memory.md). |
| [`AgentTokenStoreSweeper`](../../src/LlamaShears.Core/AgentTokenStoreSweeper.cs) | Periodically sweeps expired bearer tokens from `InMemoryAgentTokenStore`. |

Each one publishes its own logs and is observable independently. None of them blocks the system tick.

## Per-agent heartbeat: the future use case

The design intent is that the system tick will be the trigger for per-agent heartbeat firing — see [heartbeat.md](heartbeat.md). The plan:

- Each tick, every loaded agent compares `now - LastHeartbeatAt` against its `AgentConfig.HeartbeatPeriod`.
- If the period has elapsed, read `<workspace>/HEARTBEAT.md`. Non-empty → enqueue a `FrameworkUser`-roled turn with the file's content, reset `LastHeartbeatAt`. Empty → reset `LastHeartbeatAt` anyway (the period is a throttle, not a "the file must be there" requirement).

The wiring isn't in place. `AgentConfig.HeartbeatPeriod` is read by no current code. When this lands, it's a new event handler subscribed to `system:tick` (most likely on `AgentManager` or a new `HeartbeatService`), not a change to the tick service itself.

## Configuration surface

```json
{
  "Frame": {
    "Enabled": true
  }
}
```

The section name is `"Frame"` for historical reasons (the host calls each tick a "frame" internally). The option object is `SystemTickOptions`. There is no interval field — the 30s cadence is structural, not configurable, and changing it should come with a deliberate code change and a doc update here.

## Why centralize it

Three reasons:

1. **One thing to subscribe to.** Components that need a heartbeat hook against a single `EventType` instead of taking dependencies on `TimeProvider` and standing up their own timers.
2. **Observability.** Every periodic activation in the host shows up as a single log signal under `system:tick`. Easy to find in logs, easy to assert on in tests.
3. **Test substitution.** Integration tests can publish a synthetic `system:tick` to drive reconciliation without waiting wall-clock time. A `TimeProvider` substitute alone wouldn't be enough — the timer service has to receive the wake-up.

Components that *do* need their own cadence still take a `TimeProvider` (so tests can advance time) and run their own delay loop. The system tick is for housekeeping that benefits from being centrally observable.
