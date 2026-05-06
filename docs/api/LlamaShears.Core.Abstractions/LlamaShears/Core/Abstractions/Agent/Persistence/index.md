# LlamaShears.Core.Abstractions.Agent.Persistence

## Types

- [ArchiveId](ArchiveId.md) — Identifies a single archived (rotated-out) context file for an agent. The pair ([ArchiveId](ArchiveId.md).`AgentId`, [ArchiveId](ArchiveId.md).`UnixMillis`) uniquely names `<AgentId>/<UnixMillis>.json` in the context store.
- [IAgentContext](IAgentContext.md) — Live, mutable view of one agent's persisted conversation log. Backed by an [IContextStore](IContextStore.md); appending appends both in-memory and to durable storage. Snapshots of [IAgentContext](IAgentContext.md).`Turns` and [IAgentContext](IAgentContext.md).`Entries` are stable at the moment of access.
- [IContextStore](IContextStore.md) — Storage seam for per-agent conversation logs. Implementations decide the backing format (today: append-only JSON-line files on disk) and expose live [IAgentContext](IAgentContext.md) views plus archive-reading, listing, and clearing operations.

