---
name: First-class subagent_run
description: On-demand transient children via ISubagentRunner / MCP subagent_run; session-keyed locks; await suppresses parent ChannelMessage
type: project
---

`subagent_run` is a bundled MCP tool (`LlamaShears.Api.Tools.ModelContextProtocol.Subagent`) wrapping scoped `ISubagentRunner`. It uses the existing `IPromptedAgentSpawner` / `TransientAgent` path. Do not implement unused `ITransientAgentSpawner`. Do not invent a second agent loop. Do not explode `InferenceRunner`.

- **Locks.** `IAgentLock` keys on `SessionId.ToString()` (`agentId:guid:name`), not agent id. A parent turn can hold its lock while a child acquires a different key. Same session still serializes (compaction vs parent turn).
- **Await.** Default. Subscribe to child `Agent.Idle` / `Agent.Turn` before `CreateAsync`. Collect last assistant text. Set `TransientAgentReportPolicy(ReportToParent: false)` so idle does not also enqueue a parent `ChannelMessage`. Timeout (default 120s, 1–600) publishes `AgentStop` and returns `timedOut` plus any partial text.
- **Fire-and-forget.** `awaitResult=false` returns `{ started, sessionId }` immediately. `ReportToParent: true` (cron/heartbeat default when the policy is absent).
- **Nested banned.** Caller must be a root session. Same throw as the spawner.
- **Overlays.** `CreateDefaultSubAgentConfig("subagent", parent)` → `SUBAGENT.md`. Optional `model` is `CompositeIdentity.TryParse` (`provider/model`), not a URL. Optional `maxTurns` (1–64) sets child `Tools.TurnLimit`. Optional `context` prepends the prompt.
- **TurnLimit.** `AgentToolConfig.TurnLimit` is `int`, **0 = unlimited**. Do not apply the design-doc default of 8 to every agent. `IToolLoopBudget` is scoped per session; reset on inbound User / FrameworkUser. Final iteration strips the catalog; leftover calls drop at 10500 (`ToolLoopLimitMiddleware`).
- **Not a sandbox.** Child inherits parent MCP allowlist and shared workspace / memory / todos, same as cron. Not an SSRF or workspace jail.

`ok` means the awaited child finished. `error` is refuse / spawn / timeout — not the child's HTTP-style status.
