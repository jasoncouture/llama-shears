---
name: Agent turn middleware pipeline
description: Public onion around each dequeued batch; registration order is outer-to-inner; IActiveTurnCancellation / IAgentLifetime seams; inference runner is a later cleanup
type: project
---

`Agent` is the loop owner only: open context, start/stop `IAgentService`, Idle/Busy around `DequeueBatchAsync`, build `AgentPipelineContext`, `IAgentPipeline.InvokeAsync`. It does not implement `IEventHandler<>`, hold the agent lock, or own a turn CTS.

Per-batch work is a public onion under `LlamaShears.Core.Abstractions.Agent.Pipeline`. First registered `IAgentMiddleware` is **outermost**. `AddAgentMiddleware<T>()` is `TryAddEnumerable(Scoped<IAgentMiddleware, T>)` plus idempotent `TryAddScoped<IAgentPipeline, AgentPipeline>`. Host defaults in `AddAgentRuntime`, outer-to-inner: exception → activity → correlation → lock → interrupt scope → tool re-enqueue → run iteration. Plugins that `AddAgentMiddleware<T>()` after `AddCore` land inner (closest to `IAgentIterationRunner`). The terminal is a no-op; the innermost step must do the work.

Inbound events are **not** middleware. They are `IAgentService` (same as heartbeat / cron / compaction): `ChannelMessageIntakeService`, `AgentInterruptService`, `AgentShutdownService`, `AgentConfigReloadService`. Subscribe in `StartAsync`, dispose in `StopAsync`.

Two seams keep those services off `Agent`:

- `IActiveTurnCancellation` — interrupt-scope middleware `Register`s the linked turn CTS; interrupt handling calls `Cancel()`.
- `IAgentLifetime` — loop watches `Stopping`; shutdown handling and `DisposeAsync` call `Stop()`. `Stop()` does not wait and does not dispose the token.

`IAgentIterationRunner` / `IInferenceRunner` stay as they are. Do not fold stream, tool dispatch, or fragment publishing back onto `Agent`. Exploding `InferenceRunner` is a later, separate cleanup.

**How to apply:**

- New per-batch concerns become `IAgentMiddleware` (one type per file, no primary constructors on classes) registered with `AddAgentMiddleware<T>()`.
- New inbound / lifetime subscriptions become `IAgentService` registered with `AddAgentService<T>()`.
- Tests assert through public contracts (`IAgent`, `IAgentPipeline`, `ISessionQueue`, the bus). No `InternalsVisibleTo`, no log-text assertions.
- `SessionId` equality includes its generated `Guid`. Tests that need "same session" must reuse the instance (`scope.Key` / `GetCurrentSessionId()`), not `new SessionId(sameAgent, sameName)`.
