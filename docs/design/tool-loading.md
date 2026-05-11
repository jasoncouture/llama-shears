# On-demand tool loading

The agent today gets the entire tool catalog injected into the system prompt every turn. Twenty-something MCP tools, each with a name, source, description, and JSON schema, costs roughly **10 000 tokens** before the user's first message even lands. That's context the model could have spent on the actual conversation, on retrieved memories, or on tool *results* — three places where the budget is already tight.

The plan: stop shipping the catalog. Ship three **meta-tools** instead — `tool_search`, `tool_load`, `tool_unload` — and let the model pull only the tools it needs into a small active pool.

## What the model sees on a turn

```
tools = [tool_search, tool_load, tool_unload] + activePool
```

`activePool` is the per-agent set of tools the model has explicitly loaded. It is capped at **5 entries**. Crossing the cap evicts the least-recently-used entry, where "used" means "the model called it on a turn" (the model bumps its own LRU timestamps by using the tools).

The three meta-tools are always visible. They never count against the pool cap.

## The meta-tools

### `tool_search(query, limit?)`

Embeds `query` with the agent's configured embedding model, looks it up in the **tool index** (see below), returns the top-K hits as `(source, name, brief description)`. Default `limit` is 10; a hard ceiling sits at whatever the response budget allows.

The model uses this to discover what's available without having to know exact names. "I need to read a file" → `tool_search("read file")` surfaces `file_read` with its one-line description.

### `tool_load(name)`

Moves the named tool from the catalog into `activePool`. Two outcomes:

- **Pool has room.** Tool slot acquired; the next turn ships its full schema.
- **Pool full.** The least-recently-used active tool is evicted, the requested tool replaces it. The eviction is announced in the tool-call result so the model knows what it just lost.

Loading a tool that's already loaded is a no-op (refreshes its LRU timestamp).

If `name` doesn't exist in the catalog, the tool returns an error. The model is expected to fall back to `tool_search`.

### `tool_unload(name)`

Explicitly drops a tool from the active pool. Returns the new pool composition.

The model rarely needs to call this — eviction handles capacity — but it's the clean way to say "I'm done with this; make room for something else" before the LRU sweep would.

## The tool index

Per-agent, in-memory only. Built when the agent loads (or reloads). Embeddings come from the same model the agent uses for memory RAG (`AgentConfig.Embedding`).

Source of truth: the existing `IModelContextProtocolToolDiscovery` output for that agent. For each `(source, name, description, schema)` tuple, embed `name + ' — ' + description` (or a slightly richer concatenation) and stash the vector in a per-agent index. No SQLite, no disk; index dies with the agent.

Reasons memory-only is enough:

- The catalog itself is recomputed every agent (re)load — there's no point persisting a derivative of something that's already rebuilt.
- Token cost of re-indexing on load is amortized against the cost of *not* shipping 10k tokens every turn.
- Keeps the storage surface honest: the durable agent state is its memory store and its context, not its tool catalog.

## LRU bookkeeping

Each `activePool` entry carries `LastUsedAt` (a `DateTimeOffset`). Updated on:

- Successful `tool_load` (whether new slot or refresh).
- Every dispatch through the tool — the dispatcher knows the agent + tool name and stamps it on the way through.

Eviction picks the entry with the smallest `LastUsedAt`. Ties are broken arbitrarily — agents won't notice.

## Lifecycle

- **Cold start.** `activePool` is empty. First user turn, model has only the three meta-tools. It either answers the question without tools or starts with `tool_search`. One extra round-trip on the first relevant turn; ~10 k tokens saved every turn after.
- **Agent reload.** Catalog rebuilds; index rebuilds; `activePool` clears. The model goes back to cold start. Acceptable — reloads are rare and the saved tokens still dominate.
- **Compaction.** The summarizer needs the active pool intact so tool-call ↔ tool-result pairs in the surviving turns still match real loaded tools. Compaction doesn't touch the pool; it only rewrites context.

## Risks and open questions

- **Models trained on always-visible tools** may not adapt to the search-then-load workflow without a system-prompt nudge. The bundled `DEFAULT.md` template will need a section that explains the meta-tools and when to use them.
- **`tool_list` as a fourth meta-tool?** Returns the full inventory (names + one-liners, no schemas) for cases where search isn't precise enough. Token cost is moderate — names + descriptions, no schemas, no examples. Probably worth adding; deciding when the build lands.
- **Schema cost.** The bulk of the 10 k tokens is the JSON schemas, not the descriptions. Loading 5 tools restores some of that; if it turns out the schemas alone are still painful, the pool cap can drop to 3.
- **Indirect tool callers.** Code paths that drive an inference *for the agent* but aren't user-driven (cron triggers, sub-agents, heartbeat ticks) need to participate in the same loading model. The active pool is per-agent, not per-inference, so they share the slot pool with the main loop. Be explicit when wiring those features.

## Migration outline

1. New `IToolCatalog` per agent built from the existing discovery output. Holds the full inventory, the index, and the active-pool state.
2. New MCP server (built-in, not on the wire) exposing the three meta-tools. Always advertised regardless of agent config.
3. `Agent` swaps the current "full tool list" handoff to the inference runner for `metaTools + activePool`.
4. Hook the dispatcher to stamp `LastUsedAt` on dispatch.
5. System-prompt template gains a section explaining the workflow.
6. Decide on `tool_list` after observing real model behavior with the three core meta-tools.
