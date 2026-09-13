---
name: Context compaction
description: Last-6 eligible turns stay; older history is one transcript; compaction only overlays SystemPrompt and PromptContext to COMPACTION.md then next(); restore both before the user pass
type: project
---

`IContextCompactor` is policy only: split, flatten older-than-last-6 to a role-tagged transcript, persist `[system?, summary, ...preserved]`. Eligible = every `ModelTurn` except `System`, `SystemEphemeral`, and `Ephemeral == true`. Tool-cut snap-back may keep more than 6. No model calls. No template rendering.

`CompactionMiddleware` (order 7000, **before** SystemPrompt 8000 / Ephemeral 9000) double-executes `next()`:

1. Overlay `CreateDefaultSubAgentConfig("compaction", …)` so **both** `SystemPrompt` and `PromptContext` are `COMPACTION.md`. Set `Prompt = [transcript]`, `FrameworkPass`, `next()`. Do not assemble those turns.
2. Restore the original config **before** the user pass. `CommitAsync`, then `next()` with the inbound batch and `[summary + last 6]`. Idle/`/compact` sets `CompactionOnly` and stops after commit.

`CompactionAgentService` invokes the pipeline (`CompactionOnly`, optional `ForceCompaction`). Do not take `IAgentLock` first — it is not reentrant.

**How to apply:**

- New compaction templates go in `system/COMPACTION.md` and `system/context/COMPACTION.md`. Selection is `AgentConfig` overlay only.
- Do not call `ISystemPromptProvider` / `IPromptContextProvider` / `IModelPromptAssembler` from the compactor.
- `FrameworkPass` skips persist (`EmitTurns: false`) and image strip.
