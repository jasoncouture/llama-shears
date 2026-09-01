# Agent loop

What an agent actually does, turn by turn. The loop owner is [`Agent`](../../src/LlamaShears.Core/Agent.cs) (`IAgent`). Per-batch work is an onion of [`IAgentMiddleware`](../../src/public/LlamaShears.Core.Abstractions/Agent/Pipeline/IAgentMiddleware.cs). One model call lives in [`IAgentIterationRunner`](../../src/public/LlamaShears.Core.Abstractions/Agent/IAgentIterationRunner.cs). Lifetime subscriptions (intake, interrupt, shutdown, reload, heartbeat, compaction) are [`IAgentService`](../../src/public/LlamaShears.Core.Abstractions/Agent/IAgentService.cs) implementations. The host lifecycle owner is [`AgentHost`](../../src/LlamaShears.Core/AgentHost.cs) / [`IAgentFactory`](../../src/public/LlamaShears.Core.Abstractions/Agent/IAgentFactory.cs).

## Shape

Each running agent is a **scoped** `IAgent` inside an `AgentHandle`. The scope carries the session's `IDataContextScope` (`AgentConfig`, `ModelConfiguration`, `SessionPath`). The loop owner holds:

- An `IAgentContext` opened from `IContextStore.OpenAsync(sessionId)`.
- An `ISessionQueue` for the current session — inbound user turns and re-enqueued tool-result turns.
- An `IAgentPipeline` — the per-batch onion.
- An `IAgentLifetime` — the run-loop token (`Stopping`) and `Stop()` seam.
- The scoped `IEnumerable<IAgentService>` started before the loop and stopped after it.

`IAgent` exposes only `RunAsync()`. It does not subscribe to the bus, acquire the agent lock, or run inference.

## Loop

```
IAgent.RunAsync
  open IAgentContext
  publish agent:starting
  start IAgentServices
  publish agent:started
  while !IAgentLifetime.Stopping:
    Idle/Busy around ISessionQueue.DequeueBatchAsync
    new AgentPipelineContext(context, batch, shutdownToken)
    IAgentPipeline.InvokeAsync
  publish agent:stopping
  stop IAgentServices
  publish agent:stopped
```

Idle / Busy stay on the loop because they need the wait on `DequeueBatchAsync`, not a per-batch step. An empty dequeue means the queue completed (shutdown); the loop returns. `OperationCanceledException` on the lifetime token is a clean stop. Any other exception at this layer is logged and the loop retries — turn-level failures are swallowed inside the onion (`TurnExceptionMiddleware`) so they never reach here.

`DisposeAsync` calls `IAgentLifetime.Stop()` and joins the loop task. Inbound shutdown handling calls the same `Stop()` and does not wait; the loop owner joins.

## Queue → batch

[`ISessionQueue.DequeueBatchAsync`](../../src/public/LlamaShears.Core.Abstractions/Agent/Sessions/ISessionQueue.cs) is the coalesce point:

1. Drain every currently queued **tool** turn (non-blocking).
2. If any tool turns drained, also drain a same-channel **user** batch (non-blocking) and append it.
3. If no tool turns were waiting, block until at least one user turn arrives, then drain the same-channel user batch.

User turns are enqueued by [`ChannelMessageIntakeService`](../../src/LlamaShears.Core/ChannelMessageIntakeService.cs): `ChannelMessage` → `ModelTurn(User)` on this session's queue. The subscription is session-scoped (`channel:message:<session>`), so other sessions never reach `HandleAsync`.

Tool-result turns are enqueued after a non-interrupted iteration by `ToolResultEnqueueMiddleware`. That is how a tool-using turn becomes the next loop iteration — not an inner `for` on the loop owner.

## Per-batch onion

Each step has an explicit [`IAgentMiddleware.Order`](../../src/public/LlamaShears.Core.Abstractions/Agent/Pipeline/IAgentMiddleware.cs). Lowest is **outermost**. Built-ins use [`AgentMiddlewareOrder`](../../src/public/LlamaShears.Core.Abstractions/Agent/Pipeline/AgentMiddlewareOrder.cs) values spaced 1000 apart so a plugin can sit in any gap — or outside the range (`Order < 1000` is outside the exception boundary; `Order > 10000` is inside iteration). Equal orders keep enumeration order. `AddAgentRuntime` registration sequence is not what the fold uses.

```
IAgentPipeline
  1000 TurnExceptionMiddleware          after: swallow interrupt OCE and other turn failures; rethrow shutdown OCE
  2000 AgentActivityMiddleware          before: start `chat {model}` + gen_ai.* tags; after: dispose; stamp error on thrown exceptions
  3000 CorrelationScopeMiddleware       before: Guid v7 + logger scope {AgentTurnId}
  4000 AgentLockMiddleware              before: IAgentLock.AcquireLockAsync; after: dispose lock
  5000 InterruptScopeMiddleware         before: linked CTS, context.TurnToken, IActiveTurnCancellation.Register; after: unregister
  6000 ToolResultEnqueueMiddleware      after: if Outcome is not interrupted, enqueue ToolResultTurns
  7000 SystemPromptMiddleware           before: render AgentConfig.SystemPrompt → context.SystemPrompt (not persisted)
  8000 CompactionMiddleware             before: publish inbound batch, build Prompt (system + turns), CompactAsync(force: false) → context.Prompt
  9000 EphemeralContextMiddleware       before: stamp IAgentStateTracker, memory search + prompt-context template → context.EphemeralContext; insert into Prompt
  10000 RunIterationMiddleware          before: copy SessionId from the data scope, IAgentIterationRunner.RunAsync → context.Outcome; then next (no-op terminal)
```

The terminal is a no-op. The innermost step must do the work (and may still call `next`). Skipping `next` short-circuits the rest of the chain.

Public types live under `LlamaShears.Core.Abstractions.Agent.Pipeline`. Register with `AddAgentMiddleware<T>()` — it `TryAddEnumerable`s the step and idempotently registers `IAgentPipeline`. Position the step with `T.Order` (use `AgentMiddlewareOrder` as landmarks).

### Seams the onion shares with inbound services

- [`IActiveTurnCancellation`](../../src/public/LlamaShears.Core.Abstractions/Agent/Pipeline/IActiveTurnCancellation.cs) — interrupt-scope middleware `Register`s the linked turn CTS; [`AgentInterruptService`](../../src/LlamaShears.Core/AgentInterruptService.cs) calls `Cancel()`. They must not share a private field on `Agent`.
- [`IAgentLifetime`](../../src/public/LlamaShears.Core.Abstractions/Agent/Pipeline/IAgentLifetime.cs) — the loop watches `Stopping`; [`AgentShutdownService`](../../src/LlamaShears.Core/AgentShutdownService.cs) and `DisposeAsync` call `Stop()`. `Stop()` does not wait and does not dispose the token (the loop still reads it).

## One iteration (`IAgentIterationRunner`)

[`AgentIterationRunner.RunAsync`](../../src/LlamaShears.Core/AgentIterationRunner.cs) is one model call, not a TurnLimit inner loop:

1. Open a nested DI scope with the turn's data, stamp `IAgentStateTracker` (channel, correlation id, session).
2. Take `context.Prompt` (required; compaction wrote it and ephemeral middleware may have inserted into it) and `context.SessionId` (required; run-iteration middleware copied it from the data scope). Empty-response retries append a user kicker onto that same prompt — they do not re-insert ephemeral.
3. Discover MCP tools and run `IInferenceRunner.RunAsync` with empty-response retry (up to 3), passing `SessionId` and `CorrelationId` so the runner does not read the data scope. `TurnToken` cancels inference on interrupt.
4. On interrupt: return `IterationOutcome(Interrupted: true)` and do **not** produce tool-result turns.
5. On tool calls: return `Tool`-role turns in original call order. The onion enqueues them; the next dequeue feeds them back.

Inbound batch persist and compaction happen before this call, in `CompactionMiddleware`; see [compaction.md](compaction.md). Compacting first keeps the trailing user turn and leaves room for the model's reply — compacting after would rewrite the store and drop that reply.

`IAgentIterationRunner` knows nothing about the session queue, the agent lock, or interrupt subscriptions.

## Inference (still a separate engine)

[`InferenceRunner`](../../src/LlamaShears.Core/InferenceRunner.cs) is **out of this split**. It does not render a system prompt or the ephemeral block, and it does not compact. Those live on `SystemPromptMiddleware`, `EphemeralContextMiddleware`, and `CompactionMiddleware`. When compaction summarizes, it prepends `COMPACTION.md` before calling the runner. It still:

- Streams `ILanguageModel.PromptAsync`, publishes `agent:message` / `agent:thought` fragments with the iteration's correlation id, and dispatches tool calls as their fragments land ([mcp.md](mcp.md)).
- Caps consecutive tool-call rounds (`ConsecutiveToolCallLimit`, currently 15) inside that single `RunAsync`.

Exploding `IInferenceRunner` into its own onion is a later cleanup. Do not fold stream / tool / event concerns back onto `Agent`.

## Inbound services (not middleware)

These subscribe in `StartAsync` and dispose in `StopAsync`. They are registered with `AddAgentService<T>()` from `AddAgentRuntime`:

| Service | Bus event | Effect |
|---|---|---|
| `ChannelMessageIntakeService` | session-scoped `channel:message` | enqueue `ModelTurn(User)` |
| `AgentInterruptService` | session-scoped `command:interrupt-agent` | `IActiveTurnCancellation.Cancel()` |
| `AgentShutdownService` | session-scoped **and** broadcast `command:agent-shutdown` | `IAgentLifetime.Stop()` if the payload session matches or is null |
| `AgentConfigReloadService` | `lifecycle:update` keyed by agent id | `SetItem(AgentConfig.DataKey, updated)` |

Heartbeat and compaction are the same pattern (`AgentHeartbeatService`, `CompactionAgentService`) — they are not part of the per-batch onion. Compaction that must pause the loop takes `IAgentLock`, the same lock the onion holds across `next`.

Intake starting in `StartAsync` (before `agent:started`) is slightly earlier than the old in-loop subscribe. Messages that arrive during other services' `StartAsync` queue instead of being dropped.

## What's *not* in the loop owner

- **Lock / interrupt CTS / activity / correlation / tool re-enqueue / system prompt / ephemeral context / compaction / iteration.** Middleware.
- **Channel / interrupt / shutdown / config-reload handlers.** `IAgentService`.
- **`Tools.TurnLimit`.** That knob is gone. Multi-step tool use is queue → onion → enqueue tool turns → loop. Consecutive tool-call bounding lives on `InferenceRunner`.
- **`IAgent.LockAsync` / `RequestCompactionAsync`.** Those methods are gone. Lock through `IAgentLock` / `IAgentLockManager`. Compaction through `CompactionAgentService` / `IContextCompactor`.

## Tests

Contract tests live under `tests/LlamaShears.UnitTests/Agent/Pipeline/` (order, short-circuit, lock held across `next`, interrupt cancels `TurnToken` without stopping the loop, tool re-enqueue only when not interrupted) and `tests/LlamaShears.UnitTests/Agent/Core/` (loop, intake, interrupt, shutdown, reload). End-to-end coverage is `tests/LlamaShears.IntegrationTests`. Assert through public contracts (`IAgent`, `IAgentPipeline`, `ISessionQueue`, the bus). Do not add `InternalsVisibleTo`.
