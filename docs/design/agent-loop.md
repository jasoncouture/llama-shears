# Agent loop

What an agent actually does, turn by turn. The class behind this is [`Agent`](../../src/LlamaShears.Core/Agent.cs); the lifecycle owner is [`AgentManager`](../../src/LlamaShears.Core/AgentManager.cs).

## Shape

An agent is a singleton in the DI container, owned by `AgentManager`. It carries:

- A configuration snapshot (`AgentConfig`) — never mutated; reload is a Dispose-and-rebuild.
- An `IAgentContext` — the in-memory + on-disk turn list opened from `IContextStore.OpenAsync(agentId)`.
- A `Channel<IEventEnvelope<ChannelMessage>>` (unbounded, single-reader) — the inbound queue.
- A `SemaphoreSlim(1, 1)` *processing gate* — guarantees that at most one batch is in flight per agent, and lets external callers (eager compactor, slash-command handlers) stop the loop while they do something with the agent's state.
- A `Task` running `RunLoopAsync` — the loop reads the channel, batches, processes, repeats.
- A subscription to `channel:message:+` — the inbound feed.

Inbound `ChannelMessage` events whose `AgentId` doesn't match this agent's `Id` are dropped at the handler boundary (`HandleAsync`). The host can use a single bus pattern for the chat surface and let each agent self-filter.

## Per-batch flow

```
inbound channel ──▶ batch coalesce ──▶ user turn build ──▶ publish agent:turn ──▶
   resolve memories (once per batch) ──▶
      iteration loop (≤ TurnLimit):
         build prompt = system + persisted turns
         compactor.CompactAsync(force: false)        ← may rewrite prompt + persistence
         inject ephemeral prompt-context block       ← memories, time, channel id
         inferenceRunner.RunAsync                     ← streaming fragments → events
         if final iteration:                          ← TurnLimit-th iteration runs tools-less
            drop any tool calls, log empty-content if any, return
         if no tool calls:
            return
         dispatch tools in parallel
         persist Tool turns in original call order
```

Every numbered detail below points back to a line in `Agent.cs` or its callees; if you need to chase one of these to ground truth, the source is the source.

### 1. Coalesce input into one batch

`RunLoopAsync` waits on the channel reader, takes the first envelope, then drains every consecutive envelope of the *same* `EventType` (i.e. same channel id) into a single batch. The drain stops at the first peek that doesn't match. Three messages arriving back-to-back become one prompt; messages from a different channel get processed on the next iteration.

This is why coalescing lives at the loop level, not at the producer: it can only be done with knowledge of "what's already queued right now," which the producer side doesn't have.

### 2. Build one user turn from the batch

`BuildUserTurn`:

- Single message → that message's text + attachments, role `User`, timestamp from the message.
- Multiple → a header (`"The following N messages arrived since your last response, in order:"`) followed by `[1] (timestamp) text`, `[2] (timestamp) text`, … with attachments coalesced and indexed in-line so the model can correlate "image 2 of [3]" with its line. The merged turn carries the *last* message's timestamp.

The user turn is published as `agent:turn:<id>` immediately and that publication is what triggers `AgentTurnContextPersister` to write it to `current.json`. The model never sees a turn that isn't on disk.

### 3. Resolve memories once per batch

`SearchMemoriesAsync` builds a query out of the last assistant turn (if any) plus the freshly-coalesced user turn, calls `IMemorySearcher.SearchAsync` with `limit=5, minScore=0.30`, and reads the matching files. If the agent has no `WorkspacePath` or no embedding model, the call short-circuits to an empty list. If the embedding model is unreachable, the failure is logged and the turn proceeds without memory enrichment — search is best-effort.

The 0.30 floor is empirical against `embeddinggemma:latest` with the configured task prefixes; relevant matches land in the 0.40–0.60 band, noise stays under 0.10. The threshold sits in the gap.

Memories are searched **once per batch, not once per iteration**. The user input and prior assistant turn are fixed for the whole tool loop, so re-querying every iteration would be wasted work and wasted embedding-model latency.

### 4. Iterate up to `Tools.TurnLimit`

`Tools.TurnLimit` defaults to 8. The loop runs up to that many model calls per batch, with a deliberate asymmetry:

- **Iterations 1 to N-1:** prompt is sent with the discovered tool catalog. The model can emit tool calls; the loop dispatches them and re-prompts.
- **Final iteration (Nth):** prompt is sent with `PromptOptions.Tools = []`. The model has no tools available and is told via the ephemeral block: *"You have exceeded your turn limit. Respond in text — any further tool calls will be ignored. This is your final output before control returns to the user."* If the chat template still confabulates tool calls (some do, even with an empty schema), they're logged and dropped.

`TurnLimit = N` therefore means **at most N-1 tool-using turns followed by one tools-less wrap-up**. This is a structural ceiling, not a per-model behavior — it bounds how many round-trips a confused model can burn before control returns.

### 5. Compaction runs every iteration

Each iteration calls `IContextCompactor.CompactAsync(snapshot, prompt, model, modelConfig, force: false, ct)` before sending the prompt. With `force: false` the compactor short-circuits unless the token-estimate-plus-predict-budget would exceed the model's context window — so the steady state is "compaction is a no-op." When the budget is blown, it rewrites the prompt to `[system, summary, last-user-turn]`, archives `current.json` to `<unix-ms>.json`, and rebuilds it from the rebuilt prompt. See [compaction.md](compaction.md).

### 6. Inject the per-turn ephemeral block

`InjectPromptContextAsync` finds the most recent `User` turn in the prompt and inserts a `SystemEphemeral` turn immediately *before* it. The body is rendered by `IPromptContextProvider` from the `PROMPT.md` template (workspace-overridable, falls back to the bundled `content/templates/workspace/system/context/PROMPT.md`).

The block carries: current local time / timezone / day-of-week, channel id, an optional `important_message` (used for the final-iteration "tools are gone, write text" notice), the conventional workspace files (`BOOTSTRAP.md`, `IDENTITY.md`, `SOUL.md` — in that order), the memory hits, and a name-only listing of every other root-level `.md` in the workspace (so the model knows what's available without paying token cost for the bodies). See [prompt-context.md](prompt-context.md).

`SystemEphemeral` is a distinct `ModelRole` so providers can render it as a system-class message without confusing it with the persistent system prompt. It is **never persisted** — it's rebuilt each iteration so that the time, the memory hits, and the workspace file contents stay live.

### 7. Run inference

`InferenceRunner.RunAsync` consumes `ILanguageModel.PromptAsync(prompt, options)` as an `IAsyncEnumerable<IModelResponseFragment>`. As it streams it accumulates text into one `StringBuilder`, thoughts into another, and tool calls into a builder; it publishes a `FireAndForget` event for each fragment so the chat UI streams in real time. When the stream completes, it publishes a final `agent:turn` event for the assistant turn (carrying both the streamed text and the captured `ToolCalls`, which is what `AgentTurnContextPersister` writes to `current.json`).

The runner returns an `InferenceOutcome(Thinking, Content, TokenCount?, ToolCalls)`. Callers use it to decide whether to dispatch tools and whether to loop again.

### 8. Dispatch tool calls in parallel

When the outcome carries tool calls, `DispatchToolCallsAsync`:

1. Begins an `ICurrentAgentAccessor` scope carrying this agent's `AgentInfo`. The scope flows into spawned tasks because `ExecutionContext` captures `AsyncLocal` at task start.
2. Launches one `Task` per call, all running in parallel. Each task calls `IToolCallDispatcher.DispatchAsync`, captures the `ToolCallResult` into a slot in an output array, and publishes its own `agent:tool-result` event the moment it completes — so the UI can render results in arrival order regardless of which tool finished first.
3. After `Task.WhenAll`, persists `Tool`-role turns in the *original call order*. Some providers pair tool calls and tool results positionally rather than by id; deterministic order keeps re-prompting honest no matter which model is driving.

Dispatch routes by the `Source` prefix on the tool name (`server__tool` is split into `Source = "server"`, `Name = "tool"` at fragment-decode time on the provider side). If `Source` is empty or unknown, the dispatcher returns an error result rather than throwing — the loop continues and the model gets to see the failure on its next iteration. See [mcp.md](mcp.md).

The agent's `CancellationToken` flows into every dispatch. Tools are responsible for their own concurrency (locking, transactional integrity, idempotency); the framework does not serialize parallel calls.

### 9. Loop or return

After dispatching, the loop continues. It returns when any of:

- The outcome had no tool calls (the model is done; the assistant turn is the answer).
- The final iteration ran (loop body returns explicitly after the final iteration).
- The token budget gets blown and `CompactionFailedException` propagates out (rare; means the summarizer produced an empty summary).

## External entry points

The processing gate (`SemaphoreSlim`) is exposed on `IAgent`:

- **`LockAsync(ct) / UnlockAsync()`** — for callers that want to block the loop while they do something to the agent's state out-of-band. Pairs 1:1.
- **`RequestCompactionAsync(ct)`** — locks the gate, runs `IContextCompactor.CompactAsync(force: true)` against the agent's current context, releases the gate. Skips the under-budget guard so a healthy-but-aged context will still be compacted; the compactor's other guards (min turn count, no `ContextLength` configured) still apply. This is what `EagerCompactor` calls after 15 minutes of idle.

The `LastActivity` property reads the timestamp of the most recent persisted turn. The eager compactor uses it together with its own per-agent `ConcurrentDictionary<string, DateTimeOffset>` of message-fragment arrivals to decide which agents are idle.

## What's *not* in the loop

A few things you might expect from the design vocabulary that aren't here yet:

- **Heartbeat.** The agent does not handle a periodic wake-up turn. `AgentConfig.HeartbeatPeriod` is a record field with no consumer. See [heartbeat.md](heartbeat.md).
- **Reminder turns for "produce text without tools."** The design in [tool-calling.md](tool-calling.md) discusses a `ReportStatus` tool as the explicit terminator. The implemented loop uses the simpler "final iteration runs with no tools" approach instead — same end state, no extra round-trip. `ReportStatus` is not in the codebase.
- **Sub-agents.** `system/SUBAGENT.md` exists as a template, and the design vocabulary anticipates sub-agent spawning, but the loop above runs flat.

## Tests

The agent loop is exercised end-to-end by `tests/LlamaShears.IntegrationTests` using a fake `ILanguageModel`. Read those before changing batching, the iteration limit, or the order of persistence vs. dispatch — they're the regression net for behaviors that matter and are otherwise easy to break by accident.
