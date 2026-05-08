# LlamaShears

[![CLA assistant](https://cla-assistant.io/readme/badge/jasoncouture/llama-shears)](https://cla-assistant.io/jasoncouture/llama-shears)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](LICENSE.md)

An agentic hosting application for running unattended, automated agent tasks. The goal is a self-driving runtime where agents tick on a schedule, accumulate memory, retrieve relevant context, call external tools, and act without human intervention beyond initial configuration.

**This is early-stage work.** The substrate (event bus, persistence, memory + sqlite-vec RAG, MCP, agent loop, Blazor chat UI, plugin SDK chassis) is real and exercised by tests. The autonomous side — heartbeat firing, multi-LLM-provider support, channel adapters beyond the bundled UI — is partially in place; the [Status](#status) table is the source of truth on what runs end-to-end today.

## Scope

LlamaShears is a *host*, not an agent. It runs agents you configure and provides the surrounding infrastructure they need to operate continuously:

- **Heartbeat-driven execution.** Agents are scheduled by a heartbeat tick rather than invoked imperatively. The system tick is wired today; per-agent heartbeat firing from `HEARTBEAT.md` is not.
- **Persistent context** — JSON-lines files on disk for conversation turns; archive on compaction is `mv current.json <unix-ms>.json`.
- **Self-healing RAG memory** — agent-owned markdown under `memory/`, with a sqlite-vec-backed vector index (via `Microsoft.Extensions.VectorData`) that reconciles against the filesystem on a schedule.
- **Model Context Protocol (MCP) tools** — the host exposes its own MCP server (filesystem and memory tools) and dispatches outbound calls to user-configured MCP servers; tool names are source-prefixed (`server__tool`) and standardized as `<category>_<action>` on the bundled set.
- **Provider-agnostic LLM access** — Ollama and OpenAI-compatible (`/v1/chat/completions` — vLLM, llama-server, LM Studio, …) are the shipping chat providers; an in-process ONNX embeddings provider (all-MiniLM scope) ships alongside them. The abstraction lives in `LlamaShears.Core.Abstractions.Provider` and per-agent provider options layer over host defaults.
- **Plugin SDK + host loader.** `LlamaShears.Plugins` is the contract a plugin compiles against; `LlamaShears.Plugins.Host` and the `StrangeSoft.Plugins.*` pair carry the assembly-load-context chassis, host-shared type unification, transactional DI snapshot, and deferred-logger plumbing. See `samples/HelloWorld.LlamaShears.Plugin` for the minimum viable plugin shape.
- **Per-agent JSON config** — model, embedding model, MCP server allow-list, tool turn limit, heartbeat period, optional memory prefetch.
- **Server-rendered Blazor chat UI** — interactive turns over the same event bus the rest of the system speaks; auto-reload on terminal Blazor circuit reconnect failure.

## Status

| Area                          | State |
|-------------------------------|-------|
| System tick                   | **Implemented.** `SystemTickService` publishes `host:tick` on the event bus every 30s; `SystemTickOptions.Enabled` controls it. |
| Heartbeat                     | **Config-only.** `AgentConfig.HeartbeatPeriod` exists; nothing reads it yet — `HEARTBEAT.md` is not delivered to the agent on tick. |
| Agent loop                    | **Implemented.** Channel-fed, single-threaded per agent, with batching, eager parallel tool dispatch during model streaming, per-batch memory enrichment, optional pre-prompt memory prefetch, and a configurable tool turn limit. |
| Persistence                   | **Implemented.** `JsonLineContextStore` writes turns to `<Context>/<agentId>/current.json`; archives are renamed to `<unix-ms>.json` on compaction. |
| Context compaction            | **Implemented.** Auto-compacts when token estimate plus predict budget would exceed the model's context window; eager compactor force-compacts after 15 min of agent idle. Tool-call/result preservation is on the followup list (see [TASKS.md](TASKS.md)). |
| LLM providers                 | **Ollama (chat)** + **OpenAI-compatible (chat)** + **ONNX (embeddings, in-process).** The OpenAI provider speaks `/v1/chat/completions` and `/v1/models`, so it covers any OpenAI-compatible local server (vLLM, llama-server, LM Studio, TabbyAPI, …) with a free-form `ExtraRequestParams` blob for vendor knobs. Per-agent provider options layer over host defaults; `IProviderFactory.Name` and `IEmbeddingProviderFactory.Name` select between registered providers. |
| Slash commands                | **Implemented.** DI-discovered `ISlashCommand`/`ISlashCommandRegistry` from [`LlamaShears.Core.Abstractions.Commands`](src/public/LlamaShears.Core.Abstractions.Commands/). Bundled commands: `/clear`, `/archive`, `/compact`, `/restart` (graceful host re-spawn / non-zero exit for container supervisors), `/interrupt` (cancels the agent's in-flight turn while preserving persisted context). |
| Cron                          | **Stub.** Tool surface (`cron_schedule`/`list`/`edit`/`cancel`/`trigger`) and `JsonCronStore` landed; `CronExecutor` subscribes to `system:tick` and fires due jobs, but `FireSingleAsync` only logs a stub today instead of driving the agent. Gated on the channel see/unsee model — see [TASKS.md](TASKS.md). |
| Memory & RAG                  | **Implemented.** Markdown files under `<workspace>/memory/YYYY-MM-DD/<unix-seconds>.md`, SHA-256-keyed SQLite index at `<workspace>/system/.memory.db` via `Microsoft.Extensions.VectorData` + `sqlite-vec`. Index auto-rebuilds on vector-dimension mismatch; matched memories are injected as first-line summaries. |
| Memory reconciliation         | **Implemented.** `MemoryIndexerBackgroundService` walks every agent on a configured interval (default 30 min) and reconciles the index against disk. |
| MCP integration               | **Implemented.** Host MCP server mounted at `/mcp` with bearer auth; built-in tools cover filesystem and memory under `<category>_<action>` names; user-configured outbound servers are dispatched via `LoopbackBearerHandler`, with persistent `McpClient` connections pooled per server. |
| Plugin SDK                    | **Chassis implemented.** `IPlugin` contract + locator surface (`IPluginLocator<T>` / `PluginInformation` / `Plugin` orchestrator), per-plugin `AssemblyLoadContext` with host-shared type unification, deferred logger that flushes after DI is built, sample HelloWorld plugin. NuGet-source loading is followup. |
| API host                      | **Implemented.** ASP.NET Core 10 host (`LlamaShears`) with Razor Components + interactive server render mode, NerdBank.GitVersioning for build versioning, Dockerfile + compose for sandboxed runs. |
| Blazor chat UI                | **Implemented.** Server-rendered chat with streaming message/thought/tool fragments, scoped CSS, no inline JS where C# can do the job, auto-reload on terminal circuit-reconnect failure, server-side reject of reconnects during shutdown so Ctrl+C exits promptly. |
| Desktop / mobile / TUI        | **Not implemented.** Blazor server is the only UI today; followups tracked in [TASKS.md](TASKS.md). |

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
- [docs/index.md](docs/index.md) — documentation entry point.
- [docs/adr/index.md](docs/adr/index.md) — Architectural Decision Records.
- [docs/design/](docs/design/) — design notes for in-flight or recently-landed subsystems.
