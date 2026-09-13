# Context compaction

Compaction keeps the agent inside the model's context window without
replaying older history as live turns. Policy lives on
[`IContextCompactor`](../../src/public/LlamaShears.Core.Abstractions/Context/IContextCompactor.cs);
the pipeline owns inference.

## Split

Eligible turns are every `ModelTurn` except `System`, `SystemEphemeral`,
and `Ephemeral == true`. When there are more than six eligible turns,
the older prefix is flattened to one role-tagged transcript user turn
and the last six stay as real session turns. If that cut would start
the suffix on a `Tool` turn, the cut walks back to the matching
assistant call (the suffix may be longer than six). Six or fewer
eligible turns, or no configured `ContextLength`, yields no plan —
even when forced.

## What the summarizer sees

The compactor does **not** render system or ephemeral templates and
does not copy source turns into the summarizer prompt. It only
prepares the transcript. [`CompactionMiddleware`](../../src/LlamaShears.Core/Pipeline/CompactionMiddleware.cs)
(order 7000) launches an awaited child via
[`ISubagentRunner`](../../src/public/LlamaShears.Core.Abstractions/Agent/ISubagentRunner.cs):

```
Prompt        = transcript text
SystemPrompt  = COMPACTION.md
PromptContext = COMPACTION.md
MaxTurns      = 5
AwaitResult   = true
```

The child is a normal agent: its onion renders those templates.
Summarizer turns stay on the child session. The last user message is
the transcript, not a live session.
[`system/COMPACTION.md`](../../src/LlamaShears/content/templates/workspace/system/COMPACTION.md)
asks for a detailed summary plus a bulleted list of important tool
results. [`system/context/COMPACTION.md`](../../src/LlamaShears/content/templates/workspace/system/context/COMPACTION.md)
is the ephemeral overlay (`kind=compaction`).

`CommitAsync` then writes `[system?, Assistant(summary), ...preserved]`
onto the parent. The parent `next()` runs once as the user turn. Idle
/ `/compact` sets `CompactionOnly` and stops after commit.

[`CompactionAgentService`](../../src/LlamaShears.Core/CompactionAgentService.cs)
invokes the pipeline with `CompactionOnly` (and `ForceCompaction` for
`/compact`). It does not take `IAgentLock` itself — `AgentLockMiddleware`
already holds it. Nested sessions cannot spawn; a child that needs
compaction fails loudly.

## Budget (auto-compaction)

With `force: false`, a plan is prepared only when the last reported
window tokens plus predict budget plus a cheap next-turn estimate
reach `floor(window * 0.75)`.

`predictBudget` is `ModelConfiguration.TokenLimit` if set, otherwise
`Math.Max(window / 6, 256)`.

`force: true` skips that guard. The six-turn and missing-window
guards still apply.

## Rebuild

```
before:  [system?, …older eligible…, last 6 eligible…]
summarizer prompt:  [COMPACTION.md System, COMPACTION.md Ephemeral, transcript]
after:   [system?, asst(summary), …last 6…]
persisted: [summary, …last 6…]   ← system is reconstructed every batch
```

## Failure modes

- **Empty summary** or **interrupt** during the summarizer pass throws
  `CompactionFailedException`. The live store is left intact — commit
  runs only after a non-empty summary.
- Idle / `/compact` catches the exception and logs it.

## Bus events

- `agent:compacting-started:<session>` (`Awaited`) — when a plan exists.
- `agent:compacting-finished:<session>` (`Awaited`) — in `finally`.

## Constants

- **`PreserveLastTurnCount = 6`**
- **`MinTokenLimitFloor = 256`**
- **`DefaultPredictDivisor = 6`**
- **`MaxToolCallingTurns = 5`** (summarizer pass)

## Still planned

A 25% tail-budget cap (drop a tool-heavy suffix if it would consume
more than a quarter of the window) is not implemented. See
[TASKS.md](../../TASKS.md).
