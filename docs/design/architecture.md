# Architecture overview

LlamaShears is an ASP.NET Core 10 host that runs one or more autonomous agents. Each agent is a singleton in the DI container, fed by an in-process channel, persisted to disk, and able to call tools either through the host's own MCP server or through user-configured outbound MCP servers.

The codebase is split along one structural line: **`src/public/` is the NuGet-shipped surface; everything else under `src/` is host-private.** The split exists so that a plugin loaded into its own `AssemblyLoadContext` can compile against a stable, host-shared contract without picking up host wiring. Projects under `src/public/` are packable, get the strict `CS1591` / `CS1574` analyzer gate, and are the only assemblies a plugin (or any third-party consumer) is expected to reference. Projects outside `src/public/` are free to grow new dependencies; projects inside it cannot without a deliberate decision.

## Project layout

### Composition root

| Project | Role |
|---------|------|
| [src/LlamaShears](../../src/LlamaShears/) | ASP.NET Core entry point. `Program.cs`, `appsettings.json`, bundled workspace templates under `content/templates/workspace/`, the `TemplateSeedingStartupTask` that copies them into `<Templates>` on first boot, and the in-host plugin wire-up (`PathPluginLoader` + `LoadPluginsAsync`/`UsePluginsAsync`). |

### API surface

| Project | Role |
|---------|------|
| [src/LlamaShears.Api](../../src/LlamaShears.Api/) | The web/API surface added on top of an ASP.NET Core builder by `AddApi()` / `UseApi()`. Hosts the MCP server (`/mcp`), the bearer auth scheme, the bundled MCP tools (filesystem + memory), the Razor Components endpoint for the chat UI, and the shutdown-time middleware that rejects new Blazor reconnects so Ctrl+C exits promptly. |
| [src/LlamaShears.Api.Web](../../src/LlamaShears.Api.Web/) | Server-rendered Blazor chat UI: pages, layout, components (`MessageList`, `Composer`, `ChatBubbleView`, `ChatHeader`, `ReconnectModal`, `Icon`), services (`ChatSession`, `IAgentDirectory`, `MarkdownRenderer`, `IconProvider`), and `wwwroot/` static assets including the auto-reload-on-terminal-reconnect script. |

### Core implementation

| Project | Role |
|---------|------|
| [src/LlamaShears.Core](../../src/LlamaShears.Core/) | Default implementations of the contracts in `Core.Abstractions`. Owns the agent loop (`Agent`), agent lifecycle (`AgentManager`), persistence (`JsonLineContextStore`), memory (`SqliteMemoryService` over `Microsoft.Extensions.VectorData` + sqlite-vec), system-tick service, eager compactor, the MCP outbound dispatch (with `LifetimeCache`-pooled `McpClient` connections), and the in-process `LifetimeCache<TKey, TValue>` primitive. |
| [src/LlamaShears.Core.Eventing](../../src/LlamaShears.Core.Eventing/) | MessagePipe-backed `IEventBus` / `IEventPublisher` with a pattern-matched subscription wrapper and per-mode publish-side filtering via `IEventFilter`. The only project allowed to take a MessagePipe dependency. |
| [src/LlamaShears.Core.Eventing.Extensions](../../src/LlamaShears.Core.Eventing.Extensions/) | DI helpers (`AddEventHandler<T>()` etc.) for projects that want to subscribe to events without taking a direct MessagePipe dependency. |
| [src/LlamaShears.Hosting](../../src/LlamaShears.Hosting/) | Generic-host helpers: `IHostStartupTask` / `HostStartupTaskRunner`, the registration extension that runs them. Used by `TemplateSeedingStartupTask` and `AgentManager`. |

### Providers

| Project | Role |
|---------|------|
| [src/LlamaShears.Provider.Ollama](../../src/LlamaShears.Provider.Ollama/) | Ollama implementation of `IProviderFactory` (chat) and `IEmbeddingProviderFactory`. `IOllamaApiClientFactory` builds `OllamaApiClient`s from `IHttpClientFactory`-pooled handlers. Per-agent provider options layer over host defaults. |
| [src/LlamaShears.Provider.Onnx.Embeddings](../../src/LlamaShears.Provider.Onnx.Embeddings/) | In-process embeddings provider for sentence-transformers-style ONNX models (currently scoped to all-MiniLM family). Convention-based per-model layout under `<Templates>`/configured paths; pooling strategy + max-sequence-length per model. |

### Public abstractions (`src/public/`)

The `LlamaShears.Core.Abstractions` package is a metapackage with a wildcard `ProjectReference` to every sibling `LlamaShears.Core.Abstractions.*` project, so a consumer that wants the whole contract surface takes one dependency.

| Sub-project | Holds |
|-------------|-------|
| `LlamaShears.Core.Abstractions.Agent` | `AgentConfig`, `AgentInfo`, `IAgent`, `IAgentManager`, `IAgentTokenStore`, `Persistence/IContextStore`, `SystemTick`, the per-agent config records. |
| `LlamaShears.Core.Abstractions.Caching` | `IShearsCache<T>`, `IFileParserCache`, `CacheResult`. |
| `LlamaShears.Core.Abstractions.Content` | `Attachment`, `AttachmentKind` — the message-content primitives shared by Events and Provider. |
| `LlamaShears.Core.Abstractions.Context` | `AgentContext`, `IAgentContextProvider`, `IContextCompactor`, `LanguageModelContext`, `PluginContext`, `SystemContext`, `ToolContext`. |
| `LlamaShears.Core.Abstractions.Events` | `IEventBus`, `IEventPublisher`, `IEventEnvelope`, `IEventHandler<T>`, `IEventFilter`, `EventDeliveryMode`, plus the framework's typed event payloads (`AgentMessageFragment`, `AgentToolCallFragment`, `ChannelMessage`, etc.). |
| `LlamaShears.Core.Abstractions.Memory` | `IMemoryStore`, `IMemorySearcher`, `IMemoryIndexer`, `MemoryRef`, `MemoryReconciliation`, `MemorySearchResult`. |
| `LlamaShears.Core.Abstractions.Paths` | `IShearsPaths`, `PathKind`. |
| `LlamaShears.Core.Abstractions.PromptContext` | `IPromptContextProvider`, `PromptContextParameters`, `PromptContextMemory`. |
| `LlamaShears.Core.Abstractions.Provider` | `ILanguageModel`, `IEmbeddingModel`, `IProviderFactory`, `IEmbeddingProviderFactory`, `IInferenceRunner`, the model/turn/prompt/tool-call records, `ToolCall` / `ToolCallResult` / `ToolDescriptor` / `ToolGroup` / `ToolParameter`. |
| `LlamaShears.Core.Abstractions.SystemPrompt` | `ISystemPromptProvider`, `ITemplateRenderer`, `SystemPromptTemplateParameters`, `WorkspaceFile`. |

### Plugin SDK (`src/public/`)

Two pairs: a generic SDK from `StrangeSoft.*` that's reusable outside LlamaShears, and a thin LlamaShears-specific layer on top.

| Project | Role |
|---------|------|
| `StrangeSoft.Plugins.Abstractions` | The plugin-author surface for the generic chassis: `IPluginLoader<T>` (the entry point a plugin assembly exposes; the host calls it to materialize plugins of type `T`). |
| `StrangeSoft.Plugins.Host` | The host-side chassis: `PluginContext<T>`, `IPluginLocator<T>`, `PluginInformation`, `Plugin` orchestrator, `IAssemblyResolver` chain (`HostContextAssemblyResolver` for host-shared type unification, `PathAssemblyResolver` for per-plugin deps), and `IPluginContextLogger` (ILogger-style Debug/Information/Warning/Error). |
| `LlamaShears.Plugins` | The LlamaShears plugin contract: `IPlugin` with the `Register` / `Build` / `InitializeAsync` / `UnloadingAsync` lifecycle. Plugins compile against this. |
| `LlamaShears.Plugins.Host` | LlamaShears-specific DI plumbing: `LoadPluginsAsync` / `UsePluginsAsync`, transactional `IServiceCollectionSnapshot` so a failing plugin's partial registrations roll back, and the `DeferredPluginHostLogger` that buffers plugin-host log calls until DI finishes building, then flushes them onto the real `ILoggerFactory`. |

A reference plugin lives at [`samples/HelloWorld.LlamaShears.Plugin`](../../samples/HelloWorld.LlamaShears.Plugin/) — an `IPlugin` plus an `IPluginLoader<IPlugin>` that returns it.

## Reference graph

Inward-pointing arrows only; nothing in `src/public/` references anything outside `src/public/`.

```
LlamaShears (host)
└── LlamaShears.Api
    ├── LlamaShears.Core
    │   ├── public/LlamaShears.Core.Abstractions.{Agent,Caching,Content,Context,
    │   │             Events,Memory,Paths,PromptContext,Provider,SystemPrompt}
    │   └── LlamaShears.Hosting
    ├── LlamaShears.Core.Eventing            (─▶ Abstractions.Events)
    ├── LlamaShears.Core.Eventing.Extensions (─▶ Eventing)
    ├── LlamaShears.Api.Web                  (─▶ Abstractions.{Content,Events,Provider})
    ├── LlamaShears.Provider.Ollama          (─▶ Abstractions.{Content,Context,Provider})
    └── LlamaShears.Provider.Onnx.Embeddings (─▶ Abstractions.{Paths,Provider})

LlamaShears (host)
├── public/LlamaShears.Core.Abstractions     (metapackage; wildcard refs siblings)
├── public/LlamaShears.Plugins               (─▶ AspNetCore.App)
├── public/LlamaShears.Plugins.Host
│   ├── public/LlamaShears.Plugins
│   └── public/StrangeSoft.Plugins.Host (─▶ public/StrangeSoft.Plugins.Abstractions)
└── public/StrangeSoft.Plugins.Host          (host-side plugin chassis)
```

Horizontal references between abstractions sub-projects exist where one record naturally references another: `Provider → {Content, Events}` (model turns carry attachments and emit events), `Context → {Agent, Provider}` (an agent context aggregates configuration and a tool catalog), `Events → Content` (channel messages carry attachments), `Agent → Provider` (agent config references model identity). They're structural, not arbitrary.

`src/public/Directory.Build.props` carries the packable defaults (`IsPackable=true`, `WarningsAsErrors=CS1591;CS1574`) and `src/public/.editorconfig` flips the LS XML-doc analyzer family off in favour of the compiler's strict gate. Adding a new project under `src/public/` is the only signal needed to opt into the packable surface — no per-csproj `<IsPackable>true</IsPackable>` required.

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
| `<workspace>/system/.memory.db` | sqlite-vec vector index, keyed by SHA-256 of file contents. Derived from `memory/`; never authoritative — files are source of truth. Auto-rebuilds on vector-dimension mismatch. |
| `<workspace>/{BOOTSTRAP,IDENTITY,SOUL,USER,HEARTBEAT,TOOLS,MEMORY}.md` | Conventional workspace files. `BOOTSTRAP` / `IDENTITY` / `SOUL` are now consumed as part of the system prompt rather than the per-turn ephemeral block. See [agent-workspace.md](agent-workspace.md). |
| `<workspace>/system/{DEFAULT,MINIMAL,SUBAGENT}.md` | Per-agent system-prompt overrides; absence falls back to the bundled default. See [prompt-context.md](prompt-context.md). |
| `<workspace>/system/context/PROMPT.md` | Per-agent override for the per-turn ephemeral context block. |

The `<Templates>` tree is the seed for new workspaces. Editing it changes what *new* agents inherit; existing agents already have their own copy. See [paths.md](paths.md) for the configuration surface and [agent-workspace.md](agent-workspace.md) for the seeding rules.

## Cross-cutting subsystems

These five touch nearly every other component; understanding them up front makes the rest of the design read easier.

### Event bus

`IEventBus` is the only inter-component channel inside the host. Components publish typed envelopes to a colon-delimited event name (`agent:turn:<agent-id>`, `system:tick`, `channel:message:<channel-id>`, …) and subscribe with patterns (`agent:turn:+` for all agents, `agent:turn:claudia` for one). Two delivery modes:

- **`FireAndForget`** — the publisher does not await. Used for streaming UI fragments, telemetry, anything that must not block inference.
- **`Awaited`** — the publisher awaits all matching handlers. Used for persistence (the turn must be on disk before the next iteration runs).

Every publish actually fires twice — once in each mode — and handlers filter on the mode they registered with. `IEventFilter` is the publish-side gate that can suppress delivery in either mode (used today to keep noisy events out of the awaited path). See [eventing.md](eventing.md).

### System tick

`SystemTickService` publishes `system:tick` every 30 seconds (toggleable via `Frame:Enabled`). Today it drives:

- `AgentManager.ReconcileAsync` — start, reload, or stop agents based on what's in `<Data>/agents/`.
- `MemoryIndexerBackgroundService` — periodic memory reconciliation against disk.

The tick is the *only* periodic signal in the host and is intended to drive per-agent heartbeat firing as well; that wiring isn't in place yet. See [system-tick.md](system-tick.md) and [heartbeat.md](heartbeat.md).

### Agent lifecycle

`AgentManager` is the single owner of running `IAgent` instances. On `ApplicationStarted` it subscribes to `system:tick` and runs an initial reconcile; on each subsequent tick it diffs `<Data>/agents/*.json` against the loaded set and starts / reloads / stops as needed. A reconcile is gated by an `Interlocked` flag so a slow filesystem can't pile handlers up.

Each agent gets its workspace seeded (if empty) from `<Templates>/workspace/`, its tool catalog discovered from the MCP servers it whitelists, and a context handle opened from `JsonLineContextStore`.

### Authentication

The MCP listener at `/mcp` is gated by a custom bearer scheme (`AgentBearerHandler`). Tokens are minted by `InMemoryAgentTokenStore` (32 random bytes, base64), are **single-use** (the lookup `TryRemove`s the entry), and expire on a configurable lifetime. Outbound calls back into the host's own MCP listener go through `LoopbackBearerHandler`, which detects loopback URIs and mints a fresh token on the fly using `ICurrentAgentAccessor.Current`. During shutdown (`IHostApplicationLifetime.ApplicationStopping`), `LoopbackBearerHandler` short-circuits the cached `McpClient`'s teardown DELETE to a synthesized 200 so the dispose path doesn't 401-spam the log. See [mcp.md](mcp.md).

### Plugin chassis

`Plugin.LoadPluginContexts(...)` walks one or more `IPluginLocator<IPlugin>` instances, asks each for its `PluginInformation` records, and turns each record into an `IPluginContext<IPlugin>` backed by a fresh `AssemblyLoadContext`. The host-context resolver eager-loads the host's transitive reference graph into the Default ALC, then short-circuits any host-owned name to that copy — every plugin sees exactly the host's `IPlugin` / `ILogger<>` / etc. without duplication. The path resolver covers the plugin's own deps via its `deps.json`.

Plugin DI registration runs through a transactional snapshot (`IServiceCollectionSnapshot`): if `IPlugin.Register` throws, the snapshot rolls the `IServiceCollection` back so a partial registration can't ship. The `DeferredPluginHostLogger` buffers any log calls made before the DI graph is built and flushes them onto the real `ILoggerFactory` from a startup-time hosted service.

## What's wired up vs. what's intent

The substrate is complete: event bus (with publish-side `IEventFilter`), persistence, memory + RAG over `Microsoft.Extensions.VectorData` + sqlite-vec with auto-rebuild on dimension mismatch, MCP server with built-in tools (`<category>_<action>`-named) plus pooled outbound `McpClient` connections, agent loop with eager parallel tool dispatch and turn limit, context compaction (auto + eager), plugin SDK chassis with sample, Blazor chat UI streaming all of the above with auto-reload on terminal reconnect failure.

What's still on intent — recorded as design but not implemented:

- **Heartbeat firing.** `AgentConfig.HeartbeatPeriod` is a record field with a 30-minute default, but no code reads it and `HEARTBEAT.md` is never delivered to the agent. See [heartbeat.md](heartbeat.md).
- **Multi-LLM-provider.** Ollama is the only chat provider today. ONNX covers in-process embeddings. Followups for OpenAI-compatible and llama.cpp-native providers are tracked in [TASKS.md](../../TASKS.md).
- **Smarter compaction.** The current compactor can produce a window where a tool call lands without its matching result; rebuild that to safely preserve tool-call/result pairs and other invariants. Tracked in TASKS.md.
- **MCP nonce auth.** Today's bearer is a 32-byte random token tied to an `AgentInfo`. The forward design is a bearer-shaped, agent-bound, single-use nonce validated by an ASP.NET Core auth handler that emits a `ClaimsPrincipal` with agent-scoped claims. See [mcp.md](mcp.md).
- **Tool authorization (per-agent allow-lists).** Agents whitelist *MCP servers* today; per-tool grants are deferred. Tracked in TASKS.md.
- **Plugin loading from NuGet packages and per-plugin source flexibility.** The chassis loads plugins from a path today; one-config-field auto-detection across `Package.Name@SemVer`, `path/to/Assembly.dll`, `some.package.nupkg` is followup. Tracked in TASKS.md.
