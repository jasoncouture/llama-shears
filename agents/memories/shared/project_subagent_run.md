---
name: First-class subagent_run
description: On-demand transient children via ISubagentRunner / MCP subagent_run; session-keyed locks; await suppresses parent ChannelMessage
type: project
---

`subagent_run` is a bundled MCP tool (`LlamaShears.Api.Tools.ModelContextProtocol.Subagent`) wrapping scoped `ISubagentRunner`. It uses the existing `IPromptedAgentSpawner` / `TransientAgent` path. Do not implement unused `ITransientAgentSpawner`. Do not invent a second agent loop. Do not explode `InferenceRunner`.

- **Locks.** `IAgentLock` keys on `SessionId.ToString()` (`agentId:guid:name`), not agent id. A parent turn can hold its lock while a child acquires a different key. Same session still serializes (compaction vs parent turn).
- **Await.** Default. Subscribe to child `Agent.Idle` / `Agent.Turn` before `CreateAsync`. The child **must keep** `PromptedAgentStartInformation.Id` (including its `SessionId.Id` guid). `ITransientAgentFactory` must not mint a new `SessionId` from the name alone — a reminted guid means Idle never matches and the parent hangs until timeout. Collect last assistant text only when `Content.Length > 0` — a tools-only Assistant turn publishes empty content and must not wipe a prior reply. Set `TransientAgentReportPolicy(ReportToParent: false)` so idle does not also enqueue a parent `ChannelMessage`. Timeout (default 120s, 1–600) publishes `AgentStop` and returns `timedOut` plus any partial text.
- **Fire-and-forget.** `awaitResult=false` returns `{ started, sessionId }` immediately. `ReportToParent: true` (cron/heartbeat default when the policy is absent).
- **Nested banned.** Caller must be a root session. Same throw as the spawner.
- **Overlays.** `CreateDefaultSubAgentConfig("subagent", parent)` → `SUBAGENT.md`. Optional `SubagentRunRequest.SystemPrompt` / `PromptContext` replace those file names (e.g. both `COMPACTION.md`). Path separators and whitespace-only names are refused. Optional `model` is `CompositeIdentity.TryParse` (`provider/model`), not a URL. Optional `maxTurns` (1–64) sets child `Tools.TurnLimit`. Optional `context` prepends the prompt. The MCP `subagent_run` tool does not expose the template overlays; framework callers pass them on `ISubagentRunner`. Bundled `SUBAGENT.md` requires a final assistant-text result (tools-only last turn is empty `output`); do not catalog named tools in that template.
- **TurnLimit.** `AgentToolConfig.TurnLimit` is `int`, **0 = unlimited**. Do not apply the design-doc default of 8 to every agent. `IToolLoopBudget` is scoped per session; reset on inbound User / FrameworkUser. Final iteration strips the catalog; leftover calls drop at 10500 (`ToolLoopLimitMiddleware`).
- **Not a sandbox.** Child inherits parent MCP allowlist and shared workspace / memory / todos, same as cron. Not an SSRF or workspace jail.

`ok` means the requested mode succeeded (awaited idle, or fire-and-forget spawn started). `awaited` is whether the caller waited for the child to finish. Fire-and-forget success is `ok=true` + `started=true` + `awaited=false` — do not treat `ok` as "child finished." `error` is refuse / spawn / timeout — not the child's HTTP-style status.
