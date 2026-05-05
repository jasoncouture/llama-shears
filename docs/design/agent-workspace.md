# Agent workspace

Each agent has a workspace — its own directory on disk that the agent uses as a home directory. The framework recognizes a small set of conventional files; everything else in the workspace is the agent's to use as it sees fit.

## Self-contained agents

An agent is self-contained: everything that defines it — identity, persona, system prompts, memories, scratch state — lives inside its workspace. The framework supplies a starting template (see *Template seeding* below) and reads files at conventional paths, but it places no demands on the workspace's structure beyond those paths. Anything outside the conventions is the agent's own.

The conventional paths follow one consistent rule:

- **Present** → the framework loads it and uses it.
- **Absent** → either nothing is added (for files that are pure agent context, like `IDENTITY.md`), or the framework's built-in default is used (for files that drive framework behavior, like the system prompts under `system/`).

Either way, absence is never an error.

## Where the workspace lives

By default the workspace is `<Workspace>/<agent-id>/`, i.e. `<Data>/workspace/<agent-id>/` with the standard configuration. `AgentConfig.WorkspacePath` overrides that on a per-agent basis: absolute, `~/...`, or relative-to-`<Data>` are all accepted (see [agent-config.md](agent-config.md)). The directory is always created during config resolution, before the agent is loaded.

The host's bundled templates live next door at `<Templates>/workspace/`, which is itself an editable copy of the host's `content/templates/workspace/` tree. The seeding chain is *bundled → user templates root → per-agent workspace*; the rules are below.

## Conventional files

| File | Purpose |
|------|---------|
| `BOOTSTRAP.md` | One-shot setup instructions. Tells the agent to configure itself. The agent is expected to remove this file once bootstrap is complete. |
| `IDENTITY.md` | The agent's identity — name, vibe, emoji, avatar, etc. Every field is optional. |
| `SOUL.md` | Who the agent is, how it behaves, what its objectives are. |
| `USER.md` | The agent's running notes about its user — name, preferences, history, etc. |
| `HEARTBEAT.md` | Periodic heartbeat instructions (see [heartbeat.md](heartbeat.md); not yet wired). |
| `TOOLS.md` | The agent's own reference notes about how its tools work. |
| `MEMORY.md` | Short-term memory storage. |
| `AGENTS.md` | Reference notes for sub-agents (sub-agent spawning is anticipated, not yet wired). |
| `memory/YYYY-MM-DD/<unix-seconds>.md` | Long-term memory entries. The vector index lives at `system/.memory.db`. See [memory.md](memory.md). |
| `system/DEFAULT.md` | Top-level system prompt template for the agent's primary loop. |
| `system/MINIMAL.md` | Stripped-down system prompt template for short-lived or headless runs (cron, batch, single-shot tasks). |
| `system/SUBAGENT.md` | System prompt template used when the agent spawns a sub-agent. |
| `system/context/PROMPT.md` | Template for the per-turn ephemeral context block injected before the user turn. See [prompt-context.md](prompt-context.md). |

All conventional files are optional — every one of them may be missing, and the framework treats absence as "nothing to inject" or "fall back to the framework default," depending on the file (see *What the framework does with these files*).

## What the framework does with these files

The framework's job is to deliver the right file content into the agent's prompt at the right time. Beyond that, the workspace is the agent's directory: read it, write it, restructure it.

| File | Framework behavior | Implementation |
|------|--------------------|----------------|
| `BOOTSTRAP.md` | Read on every prompt and rendered into the ephemeral block (first, ahead of `IDENTITY.md` / `SOUL.md`) for as long as it exists. The agent is expected to delete the file once bootstrap is complete. | [`FilesystemPromptContextProvider`](../../src/LlamaShears.Core/PromptContext/FilesystemPromptContextProvider.cs) |
| `IDENTITY.md`, `SOUL.md` | If present, rendered into the per-turn ephemeral context block — every iteration, every batch. | [`FilesystemPromptContextProvider`](../../src/LlamaShears.Core/PromptContext/FilesystemPromptContextProvider.cs) |
| `system/DEFAULT.md`, `system/MINIMAL.md`, `system/SUBAGENT.md` | Rendered (Scriban) and used as the agent's system prompt. Selection: `AgentConfig.SystemPrompt` (defaults to `DEFAULT`). Fallback chain: workspace `<name>.md` → workspace `DEFAULT.md` → bundled `<name>.md` → bundled `DEFAULT.md`. | [`FilesystemSystemPromptProvider`](../../src/LlamaShears.Core/SystemPrompt/FilesystemSystemPromptProvider.cs) |
| `system/context/PROMPT.md` | Renders the per-turn ephemeral block. Same fallback chain as the system prompt. | [`FilesystemPromptContextProvider`](../../src/LlamaShears.Core/PromptContext/FilesystemPromptContextProvider.cs) |
| `memory/**/*.md` | Source of truth for long-term memory. The framework keeps a SQLite vector index at `system/.memory.db` in sync via on-write indexing and a periodic reconciliation scanner. | [`SqliteMemoryService`](../../src/LlamaShears.Core/Memory/SqliteMemoryService.cs) + [`MemoryIndexerBackgroundService`](../../src/LlamaShears.Core/Memory/MemoryIndexerBackgroundService.cs) |
| `system/.memory.db` | Framework-owned SQLite database. Derived; agents must not modify it directly. |  |
| `HEARTBEAT.md`, `USER.md`, `TOOLS.md`, `MEMORY.md`, `AGENTS.md`, anything else | Not currently read by the framework. Available to the agent through its filesystem tools (`file_read`, `file_write`, `file_grep`, …); the agent decides when to consult them. The system prompt and the ephemeral context block list other root-level `.md` files by *name* so the model knows what's there. | — |

The "always inject `IDENTITY` and `SOUL`" promise is delivered through the *ephemeral block*, not through the persistent system prompt. That means edits land on the next iteration without requiring a reload, and a heavily-edited agent doesn't end up paying the token cost of stale identity content from the persisted history. See [prompt-context.md](prompt-context.md).

## Agent-as-author

The workspace is read-write from the agent's perspective. Every conventional file (with the exception of `system/.memory.db`, which is framework-derived) can be created, edited, or deleted by the agent itself through whatever filesystem tools its surface exposes. This is intentional and is the basis for several of the patterns above:

- An agent removes its own `BOOTSTRAP.md` to mark setup complete.
- An agent edits `USER.md` after learning something about its user.
- An agent writes a new file under `memory/` to remember something long-term; the framework eagerly indexes it on write and the next reconciliation pass keeps the index honest. See [memory.md](memory.md).
- An agent edits or deletes a memory file; on the next memory query the orphan is filtered out, and the next reconciliation removes the index entry.

The bundled MCP filesystem tools (`file_read`, `file_list`, `file_write`, `file_append`, `file_delete`, `file_regex_replace`, `file_grep`) let the agent do all of this through its own MCP client. They resolve relative paths against the workspace root, so most agent-authored writes are workspace-local by default; absolute paths *are* honored, so an agent can read or write anywhere on disk the host process can reach. See [mcp.md](mcp.md).

The agent owning the workspace is what makes the workspace a workspace. The framework's responsibility is the small set of conventional files above; everything else in the directory is between the agent and itself.

## Template seeding

A new workspace doesn't spring out of nothing — the framework seeds it from a set of editable templates that ship with the host. Two layers, each with the same self-disabling pattern.

### Layer 1 — Bundled templates → user templates root

The host ships a bundled template tree at `src/LlamaShears/content/templates/` (built into the host's output and publish trees as Content). [`TemplateSeedingStartupTask`](../../src/LlamaShears/TemplateSeedingStartupTask.cs) runs at host startup:

1. If `<Templates>` doesn't exist, create the directory.
2. If `<Templates>` is **empty**, copy the bundled `content/templates/` into it and write a `.keep` marker.
3. If `<Templates>` contains anything — even just `.keep`, even just one stray file — leave it alone.

The user templates root is editable. The point is to give the operator a place to customize the defaults that subsequent agents will inherit, without losing those edits to the next deploy of the host.

### Layer 2 — User templates root → agent workspace

When `AgentManager` first sees an `<id>.json`, it determines the agent's workspace path (via `AgentConfigProvider`, which has already created the directory — see [agent-config.md](agent-config.md)) and calls `IDirectorySeeder.SeedIfEmpty(<Templates>/workspace/, <workspace>)`:

1. If the workspace is **empty**, copy the full `<Templates>/workspace/` tree into it and write `.keep` afterward.
2. If the workspace is non-empty, leave it alone (a debug log records the skip).
3. Either way, ensure `.keep` exists in the workspace by the end.

This means an agent's workspace is, by default, a snapshot of whatever the operator has in `<Templates>/workspace/` at the moment the agent is first loaded. From that point on, the workspace evolves on its own — the templates are a starting point, not a syncing source of truth.

### `.keep` semantics

`.keep` is the framework's "I've seen this directory" marker. Its presence in a directory is the signal *do not re-seed*.

- Empty directory (no files at all): unfilled. The framework will seed it on the next eligible boot or agent load.
- Directory containing only `.keep`: the operator (or agent) deliberately cleared the templates. The framework treats this as "intentionally empty" and leaves it alone.
- Directory containing any other files: the operator (or agent) is using it. The framework leaves it alone.

The marker is the difference between *empty because nobody has filled it yet* and *empty because someone deliberately cleared it*. Without the marker, every clear-out would be re-seeded on the next boot, defeating the operator's intent.

## Long-term memory and RAG

`memory/**/*.md` is the long-term memory tree. The markdown files on disk are the **source of truth**; the SQLite vector index at `system/.memory.db` is a derived, secondary artifact. The directory is a *self-healing* RAG document store: drift between the filesystem and the index is detected and corrected, never surfaced to the model.

The mechanics — how store/search/reconcile actually run, which embedding model is used, why the threshold sits where it does — are in [memory.md](memory.md). The contract from the agent's perspective is:

- Write a file under `memory/` (typically through `memory_store`, which auto-locates a path); it becomes searchable on the same call.
- Search via `memory_search`; you get back the matching files' contents, scored by similarity.
- Edit or delete a file; the index reconciles itself by the next query, or sooner via the periodic reconciliation scanner.

## Open items

- **`HEARTBEAT.md` consumer.** The conventional file exists and is template-seeded; nothing reads it yet. See [heartbeat.md](heartbeat.md).
- **`USER.md` lifecycle.** The convention is "agent's notes about its user." There's no framework behavior tied to it today. Whether the framework should surface it into the ephemeral context block by default (the way it does for `IDENTITY.md` and `SOUL.md`) is undecided.
- **`MEMORY.md` lifecycle.** Recorded as "short-term memory storage." Currently agent-managed; whether the framework eventually reads, writes, or compacts it is TBD.
- **Sub-agent workspaces.** `system/SUBAGENT.md` is in the seed; sub-agent spawning isn't wired. When it is, where the sub-agent's workspace lives (a subdirectory of the parent's? a sibling under `<Workspace>`?) is undecided.
- **Multi-agent collisions.** Whether two agents can share a workspace (or a `memory/` tree) is undecided. Default assumption: each agent gets its own.
