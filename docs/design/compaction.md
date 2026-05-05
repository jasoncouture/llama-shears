# Context compaction

Compaction is how the agent stays inside the model's context window without throwing away conversation history. The contract is [`IContextCompactor`](../../src/LlamaShears.Core.Abstractions/Provider/IContextCompactor.cs); the implementation is [`ContextCompactor`](../../src/LlamaShears.Core/ContextCompactor.cs) plus the [`EagerCompactor`](../../src/LlamaShears.Core/EagerCompactor.cs) background service.

## What compaction does

When triggered, compaction:

1. Calls the model with the current context (excluding the trailing user message, if any) plus a final `User`-role instruction asking it to summarize what came before.
2. Replaces the on-disk and in-memory context with `[system, summary, last-user-turn?]`. The system prompt is reconstructed each iteration anyway and is *not* persisted, so the persisted form is `[summary, last-user-turn?]`.
3. Archives the old `current.json` to `<unix-ms>.json` (see [persistence.md](persistence.md)).
4. Returns a rewritten `ModelPrompt` for the in-flight iteration to use.

The summary is an `Assistant`-role turn — written *to itself*, with the framing "this summary will be your only memory of what came before." Empirically that produces tighter, less ceremonial summaries than asking it to write for an audience.

## Two trigger paths

### Auto-compaction (per-iteration, soft)

Every iteration of the agent loop calls `_compactor.CompactAsync(snapshot, prompt, model, modelConfig, force: false, ct)`. With `force: false` the compactor short-circuits unless **all** of the following hold:

- `prompt.Turns.Count >= 5` (one system + at least two user + two assistant — below this the cost of a summarization call isn't worth what it would save).
- `ModelConfiguration.ContextLength` is set on the agent (no configured window → no budget to enforce).
- `agentContext.LanguageModel.ContextWindowTokenCount + predictBudget >= window`.
- The trailing turn is `User` or `FrameworkUser` (auto-compaction's rebuild assumes a user-anchored prompt; if the trailing turn is something else, auto-compaction skips).

Where `predictBudget` is:

- `ModelConfiguration.TokenLimit` if set, **or**
- `Math.Max(window / 6, 256)` — i.e. reserve 1/6th of the window for the model's response, with a 256-token floor.

Hitting all four conditions means the next prompt would crowd the response budget. Compact.

### Eager compaction (idle, force)

[`EagerCompactor`](../../src/LlamaShears.Core/EagerCompactor.cs) is a `BackgroundService` that watches `agent:message:+` and `agent:thought:+` events and remembers the last-seen timestamp per agent in a `ConcurrentDictionary`. Every minute it scans the dictionary; for any agent whose last activity is older than 15 minutes, it calls `IAgent.RequestCompactionAsync` (which acquires the agent's processing gate and runs `CompactAsync(force: true)`).

`force: true` skips the budget check — it's willing to compact a context that hasn't yet exceeded its window. The other guards (min turn count, missing `ContextLength`, trailing-user requirement) still apply, but with one difference: a force-compact of a non-user-trailing context summarizes *all* turns, not all-but-trailing.

The intent: an agent that's been quiet for 15 minutes is unlikely to remember the fine-grained details of its earlier conversation, so consolidating to a summary while no one is watching costs nothing and pays off the next time the agent wakes up.

The compactor's own fragment events fire under the event id `<agent-id>-compaction` so the eager compactor's `Touch` call ignores them — otherwise compaction would re-trigger compaction in a loop.

## How the rebuild works

For a typical auto-compaction (trailing turn is user-roled):

```
prompt before:
  [system, user1, asst1, tool1, user2, asst2, tool2, …, asst-N, userN]

summarization call:
  [system, user1, asst1, …, asst-N,                                       ← all but trailing user
   user("This conversation is being compacted. Summarize as notes…")]

prompt after:
  [system, asst("<summary>"), userN]                                      ← system reconstructed; persisted = [summary, userN]
```

For a force-compact triggered by the eager compactor (no trailing user turn):

```
summarization call:
  [system, user1, asst1, …, last-turn,
   user("This conversation is being compacted. Summarize as notes…")]

prompt after:
  [system, asst("<summary>")]                                             ← persisted = [summary]
```

The summary call uses `PromptOptions.TokenLimit = max(window / 3, 256)` — a hard cap on the summary length to bound the rebuilt context.

## Failure modes

- **Empty summary.** If the model returns whitespace-only content, `ContextCompactor` throws `CompactionFailedException`. The agent loop does not catch it; the iteration aborts and the caller sees the failure. (For the eager compactor, the exception is caught and logged, and the agent's last-seen timestamp is *not* re-added — so the next eager scan won't immediately retry.)
- **Cancellation mid-summary.** The agent's `CancellationToken` flows into the inference runner, which drops out cleanly. The old context is left intact — the rebuild only happens on success.
- **Compaction exception inside the agent loop.** Any exception inside `ProcessBatchAsync` is caught at the loop level and logged; the loop will retry on the next signal. Repeated empty-summary failures will look like an agent that never actually answers — visible in the logs as `CompactionFailedException`.

## Bus events

While compaction runs, the compactor publishes:

- `agent:compacting-started:<agent-id>` (`Awaited`) — at start.
- `agent:compacting-finished:<agent-id>` (`Awaited`) — in `finally`, so a failed summarization still clears any "compacting" UI state.

The chat UI subscribes to these and displays a busy indicator. Persistence does not — compaction's persistence side-effect goes through `IContextStore.ClearAsync` and `AppendAsync`, not through `agent:turn` events.

## Constants worth knowing

- **`MinTurnsForCompaction = 5`** — below this we don't compact even when forced.
- **`MinTokenLimitFloor = 256`** — minimum predict budget and summary cap.
- **`DefaultPredictDivisor = 6`** — `window / 6` when no `TokenLimit` is configured.
- **`SummaryDivisor = 3`** — `window / 3` is the hard cap on the summary length.
- **`EagerCompactor._idleThreshold = 15 minutes`** — how long agent has to be silent before eager compact.
- **`EagerCompactor._scanInterval = 1 minute`** — gap between idleness scans.

These are private `const` / `static readonly` in their respective classes — change them with a clear reason and update this doc.
