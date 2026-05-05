# Project Documentation Index

Entry point for LlamaShears design docs. Each topic is a standalone page; ADRs are kept separate because their job is "the decision and why," not "how the system works today."

## Map of the system

Read in this order if you're new:

1. [Architecture overview](design/architecture.md) — what the projects are, what depends on what, where state lives.
2. [Agent loop](design/agent-loop.md) — what an agent actually does turn-by-turn.
3. [Eventing](design/eventing.md) — the pattern-matched pub/sub bus everything else rides on.
4. [Paths and data layout](design/paths.md) — where files live on disk and which knobs move them.

## Subsystem docs

- [Agent configuration](design/agent-config.md) — the on-disk JSON, defaults, reload behavior.
- [Agent workspace](design/agent-workspace.md) — per-agent home directory; conventional files (`BOOTSTRAP.md`, `IDENTITY.md`, `SOUL.md`, `USER.md`, `HEARTBEAT.md`, `TOOLS.md`, `MEMORY.md`, `memory/`, `system/`) and what the framework does with them.
- [System prompts and prompt context](design/prompt-context.md) — Scriban templates, fallback chain, and the per-turn ephemeral block.
- [Persistence](design/persistence.md) — `JsonLineContextStore`: turns on disk, archive on compaction.
- [Context compaction](design/compaction.md) — auto-compaction on window pressure; eager compaction on idle.
- [Memory and RAG](design/memory.md) — markdown files, SQLite vector index, self-healing reconciliation.
- [Tool calling](design/tool-calling.md) — tool catalog (local + MCP), polymorphic turn model, parallel execution, per-agent iteration limits.
- [MCP server and authentication](design/mcp.md) — internal MCP listener, agent bearer auth, loopback token minting, tool source-prefix routing.
- [Heartbeat](design/heartbeat.md) — per-agent autonomous wake-up; design intent (firing on tick from `HEARTBEAT.md`) is recorded; not yet wired.
- [System tick](design/system-tick.md) — the 30s housekeeping signal that drives reconciliation today and will drive heartbeat firing.

## Architectural Decision Records

- [Architectural Decisions](adr/INDEX.md) — analyzer policy, naming conventions, file layout, and other accepted constraints. ADRs are the *decision* record; design docs are the *behavior* record. When they disagree, the code is the tiebreaker, the ADR is what's binding going forward, and the design doc gets updated.
