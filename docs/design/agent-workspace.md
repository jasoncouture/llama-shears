# Agent workspace

Each agent has a workspace — its own directory on disk that the agent uses as a home directory. The framework recognizes a small set of conventional files; everything else in the workspace is the agent's to use as it sees fit.

## Conventional files

The framework looks for these specific filenames, in the workspace root:

| File              | Purpose                                                                      |
| ----------------- | ---------------------------------------------------------------------------- |
| `BOOTSTRAP.md`    | One-shot setup instructions. Tells the agent to configure itself. The agent is expected to remove this file once bootstrap is complete. |
| `IDENTITY.md`     | The agent's identity — name, creature, vibe, emoji, avatar, etc. Every field is optional. |
| `SOUL.md`         | Who the agent is, how it behaves, what its objectives are.                   |
| `USER.md`         | The agent's running notes about its user — name, preferences, history, etc. |
| `HEARTBEAT.md`    | Periodic heartbeat instructions (see [heartbeat.md](heartbeat.md)).          |
| `TOOLS.md`        | The agent's own reference notes about how its tools work.                    |
| `MEMORY.md`       | Short-term memory storage.                                                   |
| `memories/**/*.md`| Long-term memories. Eventually backed by RAG; for now, a flat tree of markdown files (modeled on openclaw). |

All conventional files are optional — every one of them may be missing, and the framework treats absence as "nothing to inject" rather than as an error.

## What the framework does with these files

The framework's job is to deliver the right file content into the agent's prompt at the right time. Beyond that, the workspace is the agent's directory: read it, write it, restructure it.

| File                   | Framework behavior                                                                                |
| ---------------------- | ------------------------------------------------------------------------------------------------- |
| `BOOTSTRAP.md`         | If present at agent load, its contents become the **first** turn delivered to the agent. The agent is expected to delete the file as part of processing it; if it's still present at the next load, the framework will send it again. |
| `IDENTITY.md`, `SOUL.md` | If present, **always** sent as part of the agent's prompt context — every cycle, every heartbeat, every input. These are the persistent "who am I" preamble. |
| `HEARTBEAT.md`         | Read on every heartbeat firing (see [heartbeat.md](heartbeat.md)). Empty/missing → no heartbeat that interval. |
| `MEMORY.md`            | System-managed periodically. The exact lifecycle (when the framework writes/reads/compacts) is **TBD**. |
| `USER.md`, `TOOLS.md`, `memories/`, anything else | Not read by the framework. Available to the agent through its tool surface; the agent decides when to consult them. |

## Agent-as-author

The workspace is read-write from the agent's perspective. Every conventional file (with the eventual exception of system-managed `MEMORY.md`) can be created, edited, or deleted by the agent itself through whatever filesystem tools its surface exposes. This is intentional and is the basis for several of the patterns above:

- An agent removes its own `BOOTSTRAP.md` to mark setup complete.
- An agent edits `USER.md` after learning something about its user.
- An agent uses `HEARTBEAT.md` as a self-scheduling mechanism: write content to wake itself, delete the file when it has handled the request (see [heartbeat.md](heartbeat.md)'s *Silent* section).
- An agent writes a new file under `memories/` when it wants to remember something long-term.

The agent owning the workspace is what makes the workspace a workspace. The framework's responsibility is the small set of conventional files above; everything else in the directory is between the agent and itself.

## Template seeding

A new workspace doesn't spring out of nothing — the framework seeds it from a set of editable templates that ship with the host. Two layers, each with the same self-disabling pattern:

### Layer 1 — Bundled templates → user templates root

The host ships a bundled template tree at `content/templates/workspace/` (built into the host's output and publish trees as Content). On host boot:

1. If the user templates root path does not exist, create the directory.
2. If the user templates root is **empty** (no files at all), copy the bundled templates into it and write a `.keep` marker file.
3. If the user templates root contains anything — even just `.keep`, even just one stray file — leave it alone.

The user templates root is editable. The point is to give the operator a place to customize the defaults that subsequent agents will inherit, without losing those edits to the next deploy of the host.

### Layer 2 — User templates root → agent workspace

When `AgentManager` first sees an `<NAME>.json`, it determines the agent's workspace path. On agent first load:

1. If the workspace directory does not exist, create it.
2. If the workspace is **empty**, copy the full user templates root tree into it. The copy includes `.keep` as a side effect of copying everything.
3. If the workspace is non-empty, leave it alone.
4. After the copy, ensure `.keep` exists in the workspace; if for any reason it wasn't carried over, create it.

This means an agent's workspace is, by default, a snapshot of whatever the operator has in the user templates root at the moment the agent is first loaded. From that point on, the workspace evolves on its own — the templates are a starting point, not a syncing source of truth.

### `.keep` semantics

`.keep` is the framework's "I've seen this directory" marker. Its presence in a directory is the signal *do not re-seed*.

- Empty directory (no files at all): unfilled. The framework will seed it on the next eligible boot or agent load.
- Directory containing only `.keep`: the operator (or agent) deliberately cleared the templates. The framework treats this as "intentionally empty" and leaves it alone.
- Directory containing any other files: the operator (or agent) is using it. The framework leaves it alone.

The marker is the difference between *empty because nobody has filled it yet* and *empty because someone deliberately cleared it*. Without the marker, every clear-out would be re-seeded on the next boot, defeating the operator's intent.

## Long-term memory and RAG

`memories/**/*.md` is the long-term memory tree. The markdown files on disk are the **source of truth**; the vector index that backs RAG retrieval is a derived, secondary artifact. The directory is a *self-healing* RAG document store: drift between the filesystem and the index is detected and corrected on the next query, never surfaced to the model.

Each index entry carries three things alongside its vector(s): the file path, and the content hash captured at the time the file was last indexed. Retrieval uses all three:

- When the agent (or the framework on its behalf) writes a memory file under `memories/`, the framework derives a vector from the file's contents and adds it to the vector store with the file path as the key and the content hash recorded.
- When the agent retrieves memories, the framework runs the query against the vector index and gets back a set of hits keyed by file path.
- For each hit, the framework looks at the file on disk:
  - **Missing file** (agent deleted it, edited out-of-band, etc.) → the hit is dropped from the result set, noted, and queued for garbage collection of its index entry. The model never sees the orphaned hit.
  - **File present but content hash differs from the indexed hash** → the file is re-indexed in place (new vector, new hash), and the query is re-run automatically. The model only ever sees results from a vector index that matches the current file contents.
  - **File present and hash matches** → the framework reads the file and returns its content to the model.
- Garbage collection of the vector store is lazy and runs out-of-band; it's not on the retrieval hot path.

**Why this shape:**

- The agent owns its workspace (see *Agent-as-author*). "Delete the file" or "edit the file" must be the only thing the agent has to do to forget or update a memory — chasing down vector entries by hand would defeat the agent-as-author model.
- The vector store is allowed to drift behind the filesystem. As long as the framework re-reads on retrieval and re-indexes on hash mismatch, drift is invisible to the model.
- Adds are eager (write triggers indexing); deletes are lazy (retrieval surfaces orphans, GC handles the cleanup later); edits are detected on retrieval and self-heal before the result returns. That asymmetry matches where the cost lives: agents query memories often, edit them rarely, and inspect their own delete history almost never.

### Periodic reconciliation scanner

Retrieval-time healing is the *safety net*. The primary mechanism for keeping the index honest is a periodic system scan that walks the filesystem and the index together and reconciles the three diff cases:

- **New file** (file exists on disk, no entry in the index) → index it.
- **Changed file** (entry exists, file's current hash differs from the indexed hash) → re-index it.
- **Deleted file** (entry exists, file is gone) → remove the entry.

This is feasible because the vector store provides enumeration of its records — `Microsoft.Extensions.VectorData`'s collection types are `IAsyncEnumerable<TRecord>`, and the sqlite-vec backend is a regular SQLite table, so listing `(path, hash)` is a normal query. The scanner doesn't need to keep its own bookkeeping.

The cadence is intentionally not on the retrieval path. Most queries hit a fresh index because the scanner caught up between writes; the retrieval-time hash check is what handles the small window between an edit and the next scan, plus any out-of-band edits (someone opens the file in an external editor and saves).

The agent doesn't manage the vector store — it never sees the index, never invokes "reindex," never deals with vectors. From the agent's perspective, `memories/` is just a directory it can read, write, and delete files in; the framework keeps the search index honest.

## Open items

- **Per-agent workspace location.** Where each agent's workspace lives on disk (relative to the roots resolved via `IShearsPaths.GetPath(PathKind.Data | Workspace | Agents)`) is not yet wired. Two natural shapes:
  - `<Agents>/<name>/` workspace alongside `<Agents>/<name>.json` config.
  - `<Agents>/<name>/agent.json` config inside the workspace.
  - The user has the call.
- **User templates root location.** The seeding source for new agent workspaces (see *Template seeding*). Natural default: `<Data>/templates/workspace/`. Pending an explicit decision; will likely live alongside the per-agent workspace path API on `IShearsPaths`.
- **`MEMORY.md` lifecycle.** "Managed by the system periodically" — exact mechanic (compaction strategy, frequency, whether the framework rewrites or the agent does it via tool) is TBD.
- **Long-term memory format.** `memories/**/*.md` matches the existing `agents/memories/` shared/local memory layout in this repo; whether agents share that exact INDEX-plus-files convention or get a different one is TBD.
- **Multi-agent collisions.** Whether two agents can share a workspace (or a `memories/` tree) is undecided. Default assumption: each agent gets its own.
