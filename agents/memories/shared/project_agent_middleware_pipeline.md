---
name: Agent turn middleware pipeline
description: Public onion around each dequeued batch; IAgentMiddleware.Order (built-ins 1000 apart) is outer-to-inner; IActiveTurnCancellation / IAgentLifetime seams; inference runner is a later cleanup
type: project
---

`Agent` is the loop owner only: open context, start/stop `IAgentService`, Idle/Busy around `DequeueBatchAsync`, build `AgentPipelineContext`, `IAgentPipeline.InvokeAsync`. It does not implement `IEventHandler<>`, hold the agent lock, or own a turn CTS.

`AgentPipelineContext` is a **class**, not a record. Middleware mutates one instance as it walks the onion (`CorrelationId`, `TurnToken`, `SystemPrompt`, `EphemeralContext`, `Outcome`, `Batch`). A record would imply `with`-replacement and value equality; `AgentMiddlewareDelegate` is `Task`, so replacements would be dropped.

Per-batch work is a public onion under `LlamaShears.Core.Abstractions.Agent.Pipeline`. `IAgentMiddleware.Order` is **outer-to-inner** (lowest outermost). Built-ins use `AgentMiddlewareOrder` spaced 1000: TurnException 1000, AgentActivity 2000, CorrelationScope 3000, AgentLock 4000, InterruptScope 5000, ToolResultEnqueue 6000, SystemPrompt 7000, EphemeralContext 8000, RunIteration 9000. Plugins pick a value in a gap (or `< 1000` / `> 9000`). Equal orders keep enumeration order. `AddAgentMiddleware<T>()` is `TryAddEnumerable(Scoped<IAgentMiddleware, T>)` plus idempotent `TryAddScoped<IAgentPipeline, AgentPipeline>` — registration sequence is not the fold. The terminal is a no-op; the innermost step must do the work. `SystemPromptMiddleware` writes `SystemPrompt`; `EphemeralContextMiddleware` stamps `IAgentStateTracker` then writes `EphemeralContext` (memory search + prompt-context template). `IAgentIterationRunner` prepends the system turn and inserts the ephemeral turn before the last user cluster when the prompt ends in a user turn. `IInferenceRunner` does not render system or ephemeral prompts. Compaction prepends `COMPACTION.md` itself.

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
