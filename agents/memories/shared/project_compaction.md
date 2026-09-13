---
name: Context compaction
description: Last-6 eligible turns stay; older history is one transcript; summarizer is an awaited ISubagentRunner child with COMPACTION.md overlays
type: project
---

`IContextCompactor` is policy only: split, flatten older-than-last-6 to a role-tagged transcript, persist `[system?, summary, ...preserved]`. Eligible = every `ModelTurn` except `System`, `SystemEphemeral`, and `Ephemeral == true`. Tool-cut snap-back may keep more than 6. No model calls. No template rendering.

`CompactionMiddleware` (order 7000) publishes the inbound batch, then:

1. `TryPrepareAsync`. Null plan → set `Prompt` to live turns and `next()` (or return when `CompactionOnly`).
2. Else `ISubagentRunner.RunAsync` with `Prompt = transcript`, `SystemPrompt` / `PromptContext` = `COMPACTION.md`, `MaxTurns = 5`, `AwaitResult = true`. Do not overlay the parent `AgentConfig`.
3. `CommitAsync`, then `next()` once with the rebuilt prompt. Idle/`/compact` sets `CompactionOnly` and stops after commit.

`CompactionAgentService` invokes the pipeline (`CompactionOnly`, optional `ForceCompaction`). Do not take `IAgentLock` first — it is not reentrant. Nested sessions cannot spawn; compaction there fails loudly.

`context_compact` (bundled MCP) is the in-turn path: `TryPrepareAsync(force: true)` then the same `ISubagentRunner` overlays. Do **not** publish `CompactionRequest` from the tool — that re-enters the parent pipeline and deadlocks on `IAgentLock`.

**How to apply:**

- New compaction templates go in `system/COMPACTION.md` and `system/context/COMPACTION.md`. Selection is `SubagentRunRequest` overlays only.
- Do not call `ISystemPromptProvider` / `IPromptContextProvider` / `IModelPromptAssembler` from the compactor.
- Do not double-`next()` the parent onion for the summarizer.
