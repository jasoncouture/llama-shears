---
name: Agent config lifecycle and atomic-swap semantics
description: How agent configs flow from disk to interactions — manager scans top-level *.json, configs are immutable, provider serves snapshots, in-flight interactions see one snapshot end-to-end
type: project
---

The agent lifecycle has three layers, each with a defined responsibility:

1. **Disk** — `agents/*.json` (top level only — `**/*.json` is **intentionally not** the search pattern). Filename without extension is the agent's key. The user manages files; the host does not write to this directory.

2. **`AgentManager`** (singleton, `LlamaShears.Agent.Core`) — on each `SystemTick`, scans the agents directory and reconciles its in-memory set. New file → load + start. Missing file → tear down. Existing file with changed `(LastWriteTimeUtc, Length)` fingerprint → reload (parse + replace the slot). The fingerprint is captured per slot so full re-parse only happens when metadata moves; unchanged files are skipped without reading. A reload that fails to parse keeps the existing slot intact — a half-saved or syntactically broken file must not blow away a working agent. Loaded configs are stored as immutable snapshots (currently `JsonNode`, treated as immutable by convention).

3. **Agent provider** (not yet implemented) — consumes the keyed agents from the manager. Each agent base interaction fetches its config from the provider; the provider hands out the current immutable snapshot. Caching is allowed — re-parsing on every fetch is unnecessary.

**Atomic-swap invariant:** an in-flight interaction always sees a single config snapshot end-to-end. If the file is removed mid-interaction (manager tears down, provider drops the snapshot), the interaction keeps running against the snapshot it captured. New interactions get the new snapshot (or no snapshot if the agent went away). This is why config objects are immutable and why fetches happen at interaction-start, not on every property read.

**Why the directory is flat:** no nested directories in `agents/` is a deliberate UX choice. Each file is one agent. No grouping, no cascading config, no inheritance.

**How to apply:**
- New code that consumes agent configs goes through the (future) agent provider, not directly through `AgentManager`. The manager is the lifecycle authority; the provider is the read API.
- Treat `AgentSlot.Config` as immutable. Don't mutate the `JsonNode`; if a typed config record arrives later, it should be a `record` with init-only members.
- Per-file content changes are detected by the `(LastWriteTimeUtc, Length)` fingerprint. Don't add `FileSystemWatcher` or hashing — the fingerprint is intentionally cheap and tick-driven, and a same-second-same-length false-negative is accepted.
- Don't expand the search to `**/*.json`. If a future feature needs grouping, raise it; don't quietly walk subdirectories.
- The manager scans on tick only. Don't add an "initial scan in StartAsync" without raising it — the user accepted up-to-30s startup latency for first-agent-load.
