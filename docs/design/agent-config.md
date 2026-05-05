# Agent configuration

An agent is defined by a single JSON file at `<Agents>/<id>.json`. The contract is [`AgentConfig`](../../src/LlamaShears.Core.Abstractions/Agent/AgentConfig.cs); the loader is [`AgentConfigProvider`](../../src/LlamaShears.Core/AgentConfigProvider.cs).

## Schema

```json
{
  "model": {
    "id": "OLLAMA/llama3.1:latest",
    "think": "Low",
    "contextLength": 16384,
    "keepAlive": "00:30:00",
    "tokenLimit": 0
  },
  "embedding": {
    "id": "OLLAMA/embeddinggemma:latest",
    "keepAlive": "01:00:00",
    "queryPrefix": "task: search result | query: ",
    "documentPrefix": "title: none | text: "
  },
  "workspacePath": "~/agents/claudia",
  "systemPrompt": "DEFAULT",
  "promptContext": "PROMPT",
  "mcpServers": ["llamashears", "github"],
  "heartbeatPeriod": "00:30:00",
  "tools": { "turnLimit": 8 }
}
```

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| `model` | `AgentModelConfig` | ✓ | — | Chat model identity + parameters. See *Model identity* below. |
| `embedding` | `AgentEmbeddingConfig?` | | host default | Embedding model for memory RAG. Falls back to `Memory:DefaultEmbeddingModel` when absent. |
| `workspacePath` | `string?` | | `<Workspace>/<id>/` | Absolute path, `~`-prefixed home-relative path, or a relative path resolved against `<Data>`. |
| `systemPrompt` | `string?` | | `"DEFAULT"` | Template name (no extension, no path separators) under `<workspace>/system/` or the bundled fallback. |
| `promptContext` | `string?` | | `"PROMPT"` | Template name for the per-turn ephemeral block, under `<workspace>/system/context/`. |
| `mcpServers` | `string[]` | | (all known) | Whitelist of MCP server names. Omit (or set to null) to use every server the host knows about, including its own listener (`llamashears`). |
| `heartbeatPeriod` | `TimeSpan` | | `00:30:00` | Reserved. Currently unread; see [heartbeat.md](heartbeat.md). |
| `tools.turnLimit` | `int` | | `8` | Cap on tool-using iterations per batch. `N` means up to `N-1` tool turns + one final tools-less wrap-up. See [agent-loop.md](agent-loop.md). |

The `Id` field on `AgentConfig` is `[JsonIgnore]` — never write it in the JSON. The provider stamps it from the filename when it returns the config.

### Model identity

`ModelIdentity` is `Provider/Model` — the slash is the separator. `OLLAMA/llama3.1:latest` resolves to:

- Provider name: `OLLAMA` (matched case-sensitively against `IProviderFactory.Name`).
- Model name: `llama3.1:latest` (passed verbatim to the provider).

`ModelIdentityJsonConverter` deserializes the string form. The same shape applies to `embedding.id`.

### Tool turn limit

`tools.turnLimit` is the structural ceiling on how many model round-trips one user batch can drive. `8` is the default; the math is "at most 7 tool-using turns, then one tools-less response." Lower if your agent confuses itself in long tool chains; raise if it routinely needs more steps. There's no enforcement floor in code (`turnLimit: 1` would mean "always tools-less," which is a degenerate but legal config).

### Heartbeat period

`HeartbeatPeriod` is read in code but not yet acted upon. The 30-minute default is recorded as the intended rhythm; if you set it to `0` or a negative value the design says heartbeats are *disabled* for that agent. Setting any value today has no observable effect. See [heartbeat.md](heartbeat.md).

## Lifecycle

`AgentConfigProvider` reads each file lazily, on `GetConfigAsync(agentId)`. It uses `IFileParserCache<AgentConfigProvider>` so repeated reads of an unchanged file return the same parsed instance; `mtime + length` is the cache key. Edit the file and the next read picks up the new content.

`AgentManager` polls `IAgentConfigProvider.ListAgentIds()` and `GetConfigAsync(name)` on every `system:tick` (30 s) — see [system-tick.md](system-tick.md). It diffs the result against its loaded set and:

| Disk state | Loaded state | Action |
|------------|--------------|--------|
| present, parses | not loaded | `Start` (build agent, run; emit `agent:loaded`) |
| present, parses, config changed | loaded | `Reload` (build new agent, swap, dispose old) |
| missing | loaded | `Stop` (dispose) |
| present, **fails to parse** | not loaded | log warning, skip |
| present, **fails to parse** | loaded | the cached parsed config remains; the agent stays loaded |
| present, parses | loaded, **rebuild fails** | the previous agent is left intact (a build failure does not blow away a working agent) |

Reload is a *replace*, not an in-place mutate. The new agent gets a fresh inbound channel and a fresh run loop; the previous agent's `Dispose` cancels its loop and tears down its bus subscription. A reload does not clear the agent's persisted context — the new agent reopens `current.json` exactly like a fresh boot.

## Workspace path resolution

`AgentConfigProvider.ResolveWorkspacePath` is responsible for turning whatever `workspacePath` field came off disk into an absolute, trailing-separator-terminated path:

1. Empty / missing → `<Workspace>/<id>/` (creates the directory).
2. `~/...` → `$HOME/...` (creates the directory).
3. Absolute → as-is (creates the directory).
4. Anything else → relative to `<Data>` (creates the directory).

The directory is *always* created at config-resolution time, even if no agent is loaded yet. This is intentional: the seeder needs the workspace to exist before it can decide whether to seed.

## Parse failures

A failure to parse an agent config is logged and treated as "no config." The provider returns `null`; the manager's reconcile sees the agent as missing-from-disk and (if it was loaded) stops it. Don't rely on this as a kill switch — corrupting a live config to stop an agent will work, but `git checkout` to fix the typo will start it again on the next tick.

## What's *not* on `AgentConfig`

A few things you might expect that aren't in the schema:

- **Per-tool grants.** Today, tool authorization is "this agent can talk to these MCP servers." A given server's tool surface is binary: opt in to the server, get all its tools. Per-tool allow-lists are deferred.
- **Agent-level role / persona.** That's `IDENTITY.md` and `SOUL.md` in the workspace, not config. The framework reads them and ferries them into the prompt context block; it never parses them.
- **Logging level / verbosity.** Standard ASP.NET Core logging configuration applies (`Logging:LogLevel` in `appsettings.json`). Per-agent logging hasn't proved worth the configuration complexity yet.
