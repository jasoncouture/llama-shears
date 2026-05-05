# Persistence

Conversation turns are persisted as JSON-lines files on disk. There is no relational database. The contract is [`IContextStore`](../../src/LlamaShears.Core.Abstractions/Agent/Persistence/IContextStore.cs); the implementation is [`JsonLineContextStore`](../../src/LlamaShears.Core/Persistence/JsonLineContextStore.cs).

## Layout

```
<Context>/
└── <agent-id>/
    ├── current.json          # live conversation, JSON-lines
    └── 1727384712345.json    # archive, named by Unix milliseconds when archived
```

`<Context>` is `PathKind.Context`, default `<Data>/context/`. See [paths.md](paths.md).

`current.json` is the live transcript; one `IContextEntry` per line (most are `ModelTurn`, with the occasional `ModelTokenInformationContextEntry` interleaved). Lines are appended; the file is never rewritten in place.

Archives are renamed copies of `current.json` taken when the context was compacted. The filename is `<UnixMilliseconds>.json` — the millisecond at which `ClearAsync(archive: true)` ran. Archives are read-only after creation; no process appends to them.

## How turns get there

The path from "model emits a turn" to "line on disk":

1. `Agent.ProcessBatchAsync` builds a user turn (or `InferenceRunner` builds an assistant turn, or `Agent.DispatchToolCallsAsync` builds a tool turn) and publishes it as `agent:turn:<agent-id>` on the bus.
2. [`AgentTurnContextPersister`](../../src/LlamaShears.Core/Persistence/AgentTurnContextPersister.cs) is subscribed to `agent:turn:+` with `EventDeliveryMode.Awaited` — registered via `AddEventHandler<AgentTurnContextPersister>` in the host wiring (`WebApplicationBuilderExtensions.AddApi`).
3. The persister calls `IContextStore.OpenAsync(agentId)` (which returns the cached `IAgentContext`) and `IAgentContext.AppendAsync(turn)`.
4. `AgentContext.AppendAsync` updates the in-memory turn list and serializes one JSON line to `current.json` with a `FileShare.ReadWrite` open.

Because the subscription is `Awaited`, the publisher (`Agent` or `InferenceRunner`) does not return from `PublishAsync` until the line is on disk. The next iteration of the agent loop won't run until the prior turn is durable. This is the only `Awaited` subscription in the host's hot path; everything else is fire-and-forget.

## In-memory cache

`JsonLineContextStore` holds a `ConcurrentDictionary<agentId, AgentContext>`. The first `OpenAsync` for a given agent reads `current.json` line-by-line into memory; subsequent opens (including from the persister, the eager compactor, the agent's own loop) return the same cached `AgentContext`. The cache is never invalidated by file mtime — the assumption is that the framework owns the file and external edits are a misuse.

`Agent.LastActivity` reads the in-memory list directly, so the eager compactor's idleness check doesn't pay disk cost.

## Reading

Three read shapes:

- **`OpenAsync(agentId)`** → cached `IAgentContext` with the full turn list (used by the agent loop's prompt builder).
- **`ReadCurrentAsync(agentId)`** → `IAsyncEnumerable<IContextEntry>` that re-reads `current.json` from disk, line-by-line. Used by tooling that wants the fresh on-disk view (typically tests, the future archive viewer).
- **`ReadArchiveAsync(ArchiveId)`** → same shape, against the named archive file.

`ListAgentsAsync` enumerates `<Context>` subdirectories. `ListArchivesAsync(agentId)` enumerates `<unix-ms>.json` filenames in that agent's folder, parsing the timestamp out of the name.

## Writing and archiving

The persistence path is append-only. Three operations that *do* mutate disk:

- **`AgentContext.AppendAsync(entry)`** — append one JSON line to `current.json`. Called by the persister.
- **`IContextStore.ClearAsync(agentId, archive: true)`** — rename `current.json` to `<UnixMillis>.json` and clear the in-memory list. Called by `ContextCompactor` after a successful compaction. With `archive: false` (no current consumer in production), the file is deleted instead.
- **`IContextStore.DeleteAsync(ArchiveId)`** — delete a specific archive file. Reserved for future archive-management tooling; no current consumer.

There is intentionally **no `Update` and no `Delete-line`**. Once a turn is on disk it doesn't get rewritten. Everything that looks like "rewriting history" — context compaction, manual `/clear`, etc. — archives the old file and starts a fresh one.

## Ordering and durability guarantees

- **Per-agent ordering is strict.** `AppendAsync` is awaited before the next event handler runs, and the agent loop runs one iteration at a time per agent. Tool turns are persisted in original call order even though dispatch is parallel — see [agent-loop.md](agent-loop.md), step 8.
- **Write-through, no deferred flush.** `AgentContext` opens the file in append mode for each write and lets the OS handle the rest. There's no batched write or background flush; if the process is killed mid-iteration, you're left with however many turns made it past `AppendAsync`'s return.
- **No fsync.** The file is closed after each line; it is not explicitly fsynced. On a graceful shutdown the OS flushes buffers normally; on a power loss you can lose recent turns. Acceptable for a single-host development tool; if this needs to land on a server, fsync-on-append would be the change.

## What gets persisted

`IContextEntry` is the polymorphic interface — `ModelTurn` is the common case, with `ModelTokenInformationContextEntry` mixed in when the provider reports a token count. `JsonLineContextStore` deserializes via STJ; the `IContextEntry` interface uses the standard polymorphism support.

What's *not* persisted:

- **`SystemEphemeral` turns.** The per-turn ephemeral context block (memories, time, identity files) is rebuilt every iteration; persisting it would freeze stale data.
- **The system prompt.** Reconstructed from the workspace or bundled fallback every iteration.
- **Streaming fragment events** (`agent:message`, `agent:thought`, `agent:tool-call`). Only the final `ModelTurn` is persisted — the fragments are for live UI rendering.
