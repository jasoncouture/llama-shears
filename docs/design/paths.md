# Paths and data layout

LlamaShears stores all of its operator-visible state on the filesystem. There is no database. Every directory the host cares about is resolved through one interface: [`IShearsPaths`](../../src/LlamaShears.Core.Abstractions/Paths/IShearsPaths.cs).

## The `IShearsPaths` contract

```csharp
string GetPath(PathKind kind, string? subpath = null, bool ensureExists = false);
```

`PathKind` is the enum of categories the host knows about: `Data`, `Workspace`, `Agents`, `Templates`, `Context`. The default implementation [`ShearsPaths`](../../src/LlamaShears.Core/Paths/ShearsPaths.cs) materializes every root at construction time, optionally creating missing directories on the way.

`ensureExists: true` lets a caller demand the directory exist before returning. The default is `false` because most callers either know the directory will exist (they just created it) or are about to act regardless. The `ShearsPaths` constructor itself always creates roots — the choice on `GetPath` is just for the optional `subpath`.

## Defaults and overrides

The host resolves roots in this order, per kind:

1. The matching string on `ShearsPathsOptions` (if set).
2. A category-specific default derived from `PathKind.Data`.

For `PathKind.Data` itself the order is `ShearsPathsOptions.DataRoot` → `~/.llama-shears/`.

Defaults relative to `<Data>`:

| Kind | Subdirectory | `ShearsPathsOptions` field |
|------|--------------|----------------------------|
| `Data` | (the root itself) | `DataRoot` |
| `Workspace` | `workspace/` | `WorkspaceRoot` |
| `Agents` | `agents/` | `AgentsRoot` |
| `Templates` | `templates/` | `TemplatesRoot` |
| `Context` | `context/` | `ContextRoot` |

The configuration section is `Paths` by default — i.e. `appsettings.json`:

```json
{
  "Paths": {
    "DataRoot": "/var/lib/llama-shears",
    "ContextRoot": "/var/lib/llama-shears-fast/context"
  }
}
```

Any field left unset (or set to whitespace) falls through to the default. The four non-Data roots can be relocated independently — useful when, say, you want context on fast storage but agents/configs on a slower mount.

## Layout under `<Data>`

```
<Data>/
├── agents/                                # PathKind.Agents
│   ├── claudia.json                       # one file per agent, name = agent id
│   └── ...
├── templates/                             # PathKind.Templates
│   └── workspace/                         # the seed for new agent workspaces
│       ├── BOOTSTRAP.md, IDENTITY.md, ...
│       └── system/
│           ├── DEFAULT.md, MINIMAL.md, SUBAGENT.md
│           └── context/PROMPT.md
├── workspace/                             # PathKind.Workspace
│   └── <agent-id>/                        # default per-agent workspace
│       ├── (same shape as templates/workspace/, plus runtime state)
│       ├── memory/YYYY-MM-DD/<unix-seconds>.md
│       └── system/.memory.db
└── context/                               # PathKind.Context
    └── <agent-id>/
        ├── current.json                   # live JSON-lines turns
        └── <unix-ms>.json                 # compaction archives
```

A few details worth knowing:

- **Agent id is filename-derived.** `<Data>/agents/claudia.json` defines an agent with `Id = "claudia"`. The `Id` field on `AgentConfig` is `JsonIgnore`d and stamped by `AgentConfigProvider`. See [agent-config.md](agent-config.md).
- **`AgentConfig.WorkspacePath` overrides the default workspace location.** The provider supports `~/...` expansion and resolves relative paths against `<Data>`. An absolute `WorkspacePath` puts the workspace anywhere on disk.
- **`<Templates>/workspace/` is editable.** It's seeded once from the bundled tree at host boot (via `TemplateSeedingStartupTask`), then left alone forever. Operators are expected to edit it; new agents inherit whatever's there at first load.
- **`.keep` markers.** The seeder writes a zero-byte `.keep` file in any directory it seeds. The presence of that file (or any other file) tells the seeder *do not re-seed*. See [agent-workspace.md](agent-workspace.md).

## Path resolution rules

A few rules that hold across the host:

1. **Absolute paths win.** `AgentConfig.WorkspacePath = "/srv/agents/claudia"` puts the workspace there, full stop.
2. **`~` expands to `Environment.SpecialFolder.UserProfile`.** `~/agents/claudia` expands to `$HOME/agents/claudia`. `~foo` (without a slash) is *not* a tilde expansion — that's a tilde-prefixed relative name. (The agent config provider implements this — see `TryExpandHomeTilde` in `AgentConfigProvider`.)
3. **Relative paths are anchored on `<Data>`.** `WorkspacePath = "spaces/claudia"` resolves to `<Data>/spaces/claudia`. There's no per-process cwd dependency in path resolution.
4. **Workspace paths get a trailing separator.** Templates use `{{ workspace_path }}` to compose paths in the system prompt; appending a relative file works without a join.

## Configuration knobs by kind

Other knobs that interact with these paths:

- **`SystemTickOptions.Enabled`** (`Frame:Enabled`, default true) — toggles the 30s tick that drives reconciliation. With it disabled, no reconcile runs and no agents start. See [system-tick.md](system-tick.md).
- **`ModelContextProtocolOptions.Servers`** (`ModelContextProtocol:Servers`) — operator-supplied dictionary of `name → URI` for outbound MCP servers. The host's own listener is published under the fixed name `llamashears` and is appended to whatever the operator configured.
- **`MemoryServiceOptions`** (`Memory`) — host-level defaults for embedding model, query/document prefixes, and the reconciliation indexer cadence. See [memory.md](memory.md).
- **`AgentTokenStoreOptions`** (`AgentTokenStore`) — bearer token lifetime; defaults are in `AgentTokenStoreOptions`.
- **`FileParserCacheOptions`** (`FileParserCache`) — TTL on the parser cache used by template renderer + agent config provider.
- **`OllamaProviderOptions`** (`Providers:Ollama`) — `BaseUri` and `RequestTimeout` for the Ollama HTTP client.

## Why `IShearsPaths` instead of `IFileSystem` or string constants

A typed surface for "the well-known directories" makes three things easier:

- **Tests.** A test can substitute `IShearsPaths` with a fake that points all roots into a single `Path.GetTempPath()` subdirectory and tear it down at end of test.
- **Relocation.** A user that wants `<Context>` on a different mount than `<Data>` doesn't need to lift-and-shift every consumer; the four override fields cover it.
- **Discoverability.** Every consumer that calls `GetPath(PathKind.X, "subpath")` is grep-able. There's no quiet `Path.Combine(_dataRoot, "context")` hidden in some forgotten service.
