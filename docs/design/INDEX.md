# Design notes

Design-level documentation for LlamaShears subsystems. The pages here describe *how the system works today* (or, where labelled, what the implementation owes the design). ADRs cover the *decisions* that constrain those subsystems; see [docs/adr/INDEX.md](../adr/INDEX.md).

## Order to read

If you're orienting from scratch:

1. [Architecture overview](architecture.md) — projects, dependencies, where state lives.
2. [Agent loop](agent-loop.md) — what one agent does end-to-end.
3. [Eventing](eventing.md) — the bus everything else rides on.

## All pages

- [Architecture overview](architecture.md)
- [Agent loop](agent-loop.md)
- [Agent configuration](agent-config.md)
- [Agent workspace](agent-workspace.md)
- [System prompts and prompt context](prompt-context.md)
- [Persistence](persistence.md)
- [Context compaction](compaction.md)
- [Memory and RAG](memory.md)
- [Tool calling](tool-calling.md)
- [MCP server and authentication](mcp.md)
- [Heartbeat](heartbeat.md) — design intent; partially wired (config field exists, firing path does not)
- [System tick](system-tick.md)
- [Eventing](eventing.md)
- [Paths and data layout](paths.md)
