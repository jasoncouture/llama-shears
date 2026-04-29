# LlamaShears

[![CLA assistant](https://cla-assistant.io/readme/badge/jasoncouture/llama-shears)](https://cla-assistant.io/jasoncouture/llama-shears)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](LICENSE.md)

An agentic hosting application for running unattended, automated agent tasks. The goal is a self-driving runtime where agents tick on a schedule, accumulate memory, retrieve relevant context, call external tools, and act without human intervention beyond initial configuration.

**This is early-stage work.** Most of what's described below is scaffolding or aspiration; large parts aren't implemented yet. The status table calls out what is and isn't real.

## Scope

LlamaShears is intended to be a *host*, not an agent. It runs agents you configure and provides the surrounding infrastructure they need to operate continuously:

- **Heartbeat-driven execution** — agents are scheduled by a heartbeat tick rather than invoked imperatively. They decide whether to act on each tick.
- **Persistent memory** — durable session/context storage so agents survive restarts and accumulate state across runs.
- **Retrieval-augmented generation (RAG)** — vector storage and retrieval so agents can query their own past output, project documents, or external corpora.
- **Model Context Protocol (MCP)** — tool access via MCP servers so agents can take action against external systems.
- **Provider-agnostic LLM access** — Ollama-first, but every LLM provider is intended to be a first-class citizen.
- **Configurable per-agent personalities and parameters** — model choice, system prompts, tool grants, and operational schedules.
- **Cross-platform desktop UI** — likely Avalonia, with Linux treated as a first-class target rather than an afterthought.

## Status

| Area                          | State                                                                                |
|-------------------------------|--------------------------------------------------------------------------------------|
| System tick                   | Implemented. `SystemTickService` publishes `SystemTick` on MessagePipe every 30 s.     |
| Heartbeat                     | Not yet implemented. Will be a per-agent dispatcher consuming `SystemTick` and firing agents at their configured cadence. |
| Data layer (EF Core + SQLite) | Scaffolding in `LlamaShears.Data`. Sessions and session messages.                    |
| LLM providers                 | Ollama provider scaffolded. Provider abstraction in place.                           |
| Memory                        | Not yet implemented. Vector storage will use `Microsoft.Extensions.VectorData`.      |
| RAG                           | Not yet implemented.                                                                 |
| MCP integration               | Not yet implemented.                                                                 |
| API host                      | Minimal ASP.NET surface in `LlamaShears.Api.Host`.                                   |
| Desktop UI                    | Not yet implemented. Likely Avalonia, with Linux as a first-class target.            |

## Building and running

```sh
dotnet tool restore
dotnet build
dotnet test
```

The project targets .NET 10. The `dotnet-ef` migration tool is pinned via `dotnet-tools.json`.

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

- [docs/INDEX.md](docs/INDEX.md) — documentation entry point.
- [docs/adr/INDEX.md](docs/adr/INDEX.md) — Architectural Decision Records.
- [docs/design/](docs/design/) — design notes for in-flight or recently-landed subsystems.
