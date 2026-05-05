# Architecture overview

LlamaShears is an ASP.NET Core 10 host that runs one or more autonomous agents. Each agent is a singleton in the DI container, fed by an in-process channel, persisted to disk, and able to call tools either through the host's own MCP server or through user-configured outbound MCP servers.

The codebase is split into nine projects. The split exists for one purpose: keep the *contracts* a future plugin (loaded into its own `AssemblyLoadContext`) needs to consume free of host wiring. The non-`Abstractions` projects can grow new dependencies; `*.Abstractions` cannot without a deliberate decision.

## Projects

| Project | Role |
|---------|------|
| [src/LlamaShears](../../src/LlamaShears/) | Composition root. ASP.NET Core entry point, `appsettings.json`, bundled workspace templates under `content/templates/workspace/`, and the `TemplateSeedingStartupTask` that copies them into `<Templates>` on first boot. |
| [src/LlamaShears.Api](../../src/LlamaShears.Api/) | The web/API surface added on top of an ASP.NET Core builder by `AddApi()` / `UseApi()`. Hosts the MCP server (`/mcp`), the bearer auth scheme, the bundled MCP tools (filesystem + memory), and the Razor Components endpoint for the chat UI. |
| [src/LlamaShears.Api.Web](../../src/LlamaShears.Api.Web/) | Server-rendered Blazor chat UI: pages, layout, components (`MessageList`, `Composer`, `ChatBubbleView`, `ChatHeader`, `ReconnectModal`, `Icon`), services (`ChatSession`, `IAgentDirectory`, `MarkdownRenderer`, `IconProvider`), and `wwwroot/` static assets. |
| [src/LlamaShears.Core](../../src/LlamaShears.Core/) | Default implementations of the contracts in `Abstractions`. Owns the agent loop (`Agent`), agent lifecycle (`AgentManager`), persistence, memory, system-tick service, eager compactor, and the MCP outbound dispatch. |
| [src/LlamaShears.Core.Abstractions](../../src/LlamaShears.Core.Abstractions/) | Interfaces, DTOs, and event payload records. No project references. This is the contract surface a plugin would compile against. |
| [src/LlamaShears.Core.Eventing](../../src/LlamaShears.Core.Eventing/) | MessagePipe-backed `IEventBus` / `IEventPublisher` with a pattern-matched subscription wrapper. The only project allowed to take a MessagePipe dependency. |
| [src/LlamaShears.Core.Eventing.Extensions](../../src/LlamaShears.Core.Eventing.Extensions/) | DI helpers (`AddEventHandler<T>()` etc.) for projects that want to subscribe to events without taking a direct MessagePipe dependency. |
| [src/LlamaShears.Hosting](../../src/LlamaShears.Hosting/) | Generic-host helpers: `IHostStartupTask` / `HostStartupTaskRunner`, the registration extension that runs them. Used by `TemplateSeedingStartupTask` and `AgentManager`. |
| [src/LlamaShears.Provider.Ollama](../../src/LlamaShears.Provider.Ollama/) | Ollama implementation of `IProviderFactory` (chat) and `IEmbeddingProviderFactory`. Wraps `OllamaSharp` and translates its streaming output into `IModelResponseFragment` instances. |

## Reference graph

```
LlamaShears (host)
└── LlamaShears.Api
    ├── LlamaShears.Core
    │   ├── LlamaShears.Core.Abstractions
    │   └── LlamaShears.Hosting
    ├── LlamaShears.Core.Eventing            (─▶ Abstractions)
    ├── LlamaShears.Core.Eventing.Extensions (─▶ Eventing)
    ├── LlamaShears.Api.Web                  (─▶ Abstractions)
    └── LlamaShears.Provider.Ollama          (─▶ Abstractions)
```

`Abstractions` has no project references; everything else points inward toward it. `Core.Eventing` is the only assembly that may add a MessagePipe reference (see [eventing.md](eventing.md)). `Hosting` is shared infrastructure used by both the composition root and `Core`.

## Where state lives

LlamaShears is filesystem-first. There is no relational database; everything an operator might want to back up or grep is plain files under `<Data>` (default `~/.llama-shears/`).

| Path | Contents |
|------|----------|
| `<Data>/agents/<id>.json` | Per-agent configuration. Watched and reloaded when the file mtime changes. |
| `<Data>/templates/workspace/...` | Editable copy of the bundled templates. Seeded once on host boot. New agent workspaces are seeded from this tree. |
| `<Data>/workspace/<id>/` | Default per-agent workspace if `AgentConfig.WorkspacePath` is not set. Seeded from `<Templates>/workspace/`. |
| `<Data>/context/<id>/current.json` | Live conversation turns as JSON-lines. |
| `<Data>/context/<id>/<unix-ms>.json` | Compaction archives — the previous `current.json` renamed when context was compacted. |
| `<workspace>/memory/YYYY-MM-DD/<unix-seconds>.md` | Long-term memory files. Agent-owned. |
| `<workspace>/system/.memory.db` | SQLite vector index, derived from `memory/`. Never authoritative — files are source of truth. |
| `<workspace>/{BOOTSTRAP,IDENTITY,SOUL,USER,HEARTBEAT,TOOLS,MEMORY}.md` | Conventional workspace files. See [agent-workspace.md](agent-workspace.md). |
| `<workspace>/system/{DEFAULT,MINIMAL,SUBAGENT}.md` | Per-agent system-prompt overrides; absence falls back to the bundled default. See [prompt-context.md](prompt-context.md). |
| `<workspace>/system/context/PROMPT.md` | Per-agent override for the per-turn ephemeral context block. |

The `<Templates>` tree is the seed for new workspaces. Editing it changes what *new* agents inherit; existing agents already have their own copy. See [paths.md](paths.md) for the configuration surface and [agent-workspace.md](agent-workspace.md) for the seeding rules.

## Cross-cutting subsystems

These four touch nearly every other component; understanding them up front makes the rest of the design read easier.

### Event bus

`IEventBus` is the only inter-component channel inside the host. Components publish typed envelopes to a colon-delimited event name (`agent:turn:<agent-id>`, `host:tick`, `channel:message:<channel-id>`, …) and subscribe with patterns (`agent:turn:+` for all agents, `agent:turn:claudia` for one). Two delivery modes:

- **`FireAndForget`** — the publisher does not await. Used for streaming UI fragments, telemetry, anything that must not block inference.
- **`Awaited`** — the publisher awaits all matching handlers. Used for persistence (the turn must be on disk before the next iteration runs).

Every publish actually fires twice — once in each mode — and handlers filter on the mode they registered with. This is what lets a single event have both a hot UI fan-out and a slow disk persistence handler without either blocking the other. See [eventing.md](eventing.md).

### System tick

`SystemTickService` publishes `host:tick` every 30 seconds (toggleable via `Frame:Enabled`). Today it drives:

- `AgentManager.ReconcileAsync` — start, reload, or stop agents based on what's in `<Data>/agents/`.

The tick is the *only* periodic signal in the host and is intended to drive per-agent heartbeat firing as well; that wiring isn't in place yet. See [system-tick.md](system-tick.md) and [heartbeat.md](heartbeat.md).

### Agent lifecycle

`AgentManager` is the single owner of running `IAgent` instances. On `ApplicationStarted` it subscribes to `host:tick` and runs an initial reconcile; on each subsequent tick it diffs `<Data>/agents/*.json` against the loaded set and starts / reloads / stops as needed. A reconcile is gated by an `Interlocked` flag so a slow filesystem can't pile handlers up.

Each agent gets its workspace seeded (if empty) from `<Templates>/workspace/`, its tool catalog discovered from the MCP servers it whitelists, and a context handle opened from `JsonLineContextStore`.

### Authentication

The MCP listener at `/mcp` is gated by a custom bearer scheme (`AgentBearerHandler`). Tokens are minted by `InMemoryAgentTokenStore` (32 random bytes, base64), are **single-use** (the lookup `TryRemove`s the entry), and expire on a configurable lifetime. Outbound calls back into the host's own MCP listener go through `LoopbackBearerHandler`, which detects loopback URIs and mints a fresh token on the fly using `ICurrentAgentAccessor.Current`. See [mcp.md](mcp.md).

## What's wired up vs. what's intent

The substrate is complete: event bus, persistence, memory + RAG (raw SQLite, not yet `Microsoft.Extensions.VectorData`), MCP server with built-in tools, agent loop with parallel tool dispatch and turn limit, context compaction (auto + eager), Blazor chat UI streaming all of the above.

What's still on intent — recorded as design but not implemented:

- **Heartbeat firing.** `AgentConfig.HeartbeatPeriod` is a record field with a 30-minute default, but no code reads it and `HEARTBEAT.md` is never delivered to the agent. See [heartbeat.md](heartbeat.md).
- **Vector store migration.** Memory uses `Microsoft.Data.Sqlite` directly with cosine similarity in C#. The plan is to switch to `Microsoft.Extensions.VectorData` + sqlite-vec.
- **Multi-provider.** The provider abstraction is there; only Ollama implements it.
- **MCP nonce auth.** Today's bearer is a 32-byte random token tied to an `AgentInfo`. The forward design is a bearer-shaped, agent-bound, single-use nonce validated by an ASP.NET Core auth handler that emits a `ClaimsPrincipal` with agent-scoped claims. See [mcp.md](mcp.md).
- **Tool authorization (per-agent allow-lists).** Agents whitelist *MCP servers* today; per-tool grants are deferred.
