# LlamaShears

[![CLA assistant](https://cla-assistant.io/readme/badge/jasoncouture/llama-shears)](https://cla-assistant.io/jasoncouture/llama-shears)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](LICENSE.md)

An agentic hosting application for running unattended, automated agent tasks. The goal is a self-driving runtime where agents tick on a schedule, accumulate memory, retrieve relevant context, call external tools, and act without human intervention beyond initial configuration.

**This is early-stage work.** The substrate (event bus, persistence, memory, MCP, agent loop, Blazor chat UI) is real and exercised by tests. The autonomous side — heartbeat firing, RAG-driven recall on its own schedule, multi-provider support — is partially in place; the [Status](#status) table is the source of truth on what runs end-to-end today.

## Scope

LlamaShears is a *host*, not an agent. It runs agents you configure and provides the surrounding infrastructure they need to operate continuously:

- **Heartbeat-driven execution.** Agents are scheduled by a heartbeat tick rather than invoked imperatively. The system tick is wired today; per-agent heartbeat firing from `HEARTBEAT.md` is not.
- **Persistent context** — JSON-lines files on disk for conversation turns; archive on compaction is `mv current.json <unix-ms>.json`.
- **Self-healing RAG memory** — agent-owned markdown under `memory/`, with a SQLite-backed vector index that reconciles against the filesystem on a schedule.
- **Model Context Protocol (MCP) tools** — the host exposes its own MCP server (filesystem and memory tools) and dispatches outbound calls to user-configured MCP servers; tool names are source-prefixed (`server__tool`).
- **Provider-agnostic LLM access** — Ollama is the only shipping provider; the abstraction is in `LlamaShears.Core.Abstractions.Provider`.
- **Per-agent JSON config** — model, embedding model, MCP server allow-list, tool turn limit, heartbeat period.
- **Server-rendered Blazor chat UI** — interactive turns over the same event bus the rest of the system speaks.

## Status

| Area                          | State |
|-------------------------------|-------|
| System tick                   | **Implemented.** `SystemTickService` publishes `system:tick` on the event bus every 30s; `SystemTickOptions.Enabled` controls it. |
| Heartbeat                     | **Config-only.** `AgentConfig.HeartbeatPeriod` exists; nothing reads it yet — `HEARTBEAT.md` is not delivered to the agent on tick. |
| Agent loop                    | **Implemented.** Channel-fed, single-threaded per agent, with batching, parallel tool dispatch, per-batch memory enrichment, and a configurable tool turn limit. |
| Persistence                   | **Implemented.** `JsonLineContextStore` writes turns to `<Context>/<agentId>/current.json`; archives are renamed to `<unix-ms>.json` on compaction. |
| Context compaction            | **Implemented.** Auto-compacts when token estimate plus predict budget would exceed the model's context window; eager compactor force-compacts after 15 min of agent idle. |
| LLM providers                 | **Ollama only.** Provider factory + embeddings; `IProviderFactory.Name` selects between providers. |
| Memory & RAG                  | **Implemented (raw SQLite).** Markdown files under `<workspace>/memory/YYYY-MM-DD/<unix-seconds>.md`, SHA-256-keyed SQLite index at `<workspace>/system/.memory.db`. Cosine similarity is computed in-process; the move to `Microsoft.Extensions.VectorData` + sqlite-vec is still planned. |
| Memory reconciliation         | **Implemented.** `MemoryIndexerBackgroundService` walks every agent on a configured interval (default 30 min) and reconciles the index against disk. |
| MCP integration               | **Implemented.** Host MCP server mounted at `/mcp` with bearer auth; built-in tools cover filesystem and memory; user-configured outbound servers are dispatched via `LoopbackBearerHandler`. |
| API host                      | **Implemented.** ASP.NET Core 10 host (`LlamaShears`) with Razor Components + interactive server render mode. |
| Blazor chat UI                | **Implemented.** Server-rendered chat with streaming message/thought/tool fragments, scoped CSS, no inline JS where C# can do the job. |
| Desktop UI                    | **Not implemented.** Blazor server is the only UI today. |

## Building and running

```sh
dotnet build
dotnet test
```

The project targets .NET 10. The shipping host is `src/LlamaShears` (`dotnet run --project src/LlamaShears` for a local Kestrel run); configuration lives in `src/LlamaShears/appsettings.json`. Default data root is `~/.llama-shears/` — override `Paths:DataRoot` (or any of `WorkspaceRoot`, `AgentsRoot`, `TemplatesRoot`, `ContextRoot`) in configuration to relocate it.

Agents are configured by dropping `<agent-id>.json` into `<Data>/agents/`. Minimum schema:

```json
{
  "model": { "id": "OLLAMA/llama3.1:latest" },
  "mcpServers": ["llamashears"]
}
```

See [docs/design/agent-config.md](docs/design/agent-config.md) for the full schema.

## Licensing

LlamaShears is licensed under [AGPL-3.0-or-later](LICENSE.md). Individuals, hobbyists, freelancers, small operators, and anyone who can comply with AGPL terms are welcome to use it without a commercial license.

A separate **commercial license** is available for parties whose use case is incompatible with AGPL — see [COMMERCIAL.md](COMMERCIAL.md) for the rationale, intent, and inquiry path. The dual-licensing setup is a defense against the well-documented pattern of large companies free-riding on open source. The intent is to stop predatory corporate behavior, not to extract money from individual users or contributors.

## Contributing

Contributions are welcome. The first-time contribution flow:

1. Read [CONTRIBUTING.md](CONTRIBUTING.md) for repository layout, code style, build/test expectations, and commit conventions.
2. Sign the [Contributor License Agreement](CLA.md) when prompted by the CLA Assistant bot on your first PR.
3. Open a PR.

The CLA exists so the project can offer commercial licenses to companies that need them. It does not change your rights to your own contributions or imply any compensation arrangement; the full reasoning is in [COMMERCIAL.md](COMMERCIAL.md).

Most style and policy is enforced as hard compile errors by the in-tree analyzers under [analyzers/](analyzers/). The accepted decisions and rationale are documented as ADRs under [docs/adr/](docs/adr/).

## Documentation

- [PRINCIPLES.md](PRINCIPLES.md) — the two principles that generate the project's concrete rules.
- [docs/INDEX.md](docs/INDEX.md) — documentation entry point.
- [docs/adr/INDEX.md](docs/adr/INDEX.md) — Architectural Decision Records.
- [docs/design/](docs/design/) — design notes for in-flight or recently-landed subsystems.
