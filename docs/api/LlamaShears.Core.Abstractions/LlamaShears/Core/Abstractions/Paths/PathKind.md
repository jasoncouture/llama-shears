# LlamaShears.Core.Abstractions.Paths.PathKind

Assembly: `LlamaShears.Core.Abstractions`

Well-known categories of host state whose on-disk root is
resolved by [IApplicationPathProvider](IApplicationPathProvider.md). Implementations decide
where each root lives and whether to create directories on demand.

## Fields

### `AgentSkills`

The per-agent skill root located inside the agent's workspace
([PathKind](PathKind.md).`Workspace`/`<subpath>`/`skills`). Skills installed here are
only visible to the agent whose workspace contains them.

### `Agents`

The directory holding per-agent `<id>.json` configuration files.

### `AppSkills`

The host-scoped skill root that lives under the framework data directory
([PathKind](PathKind.md).`Data`). Skills installed here are shared by every agent of
this host but isolated from other hosts on the machine.

### `Context`

The directory holding per-agent persisted conversation logs (the "context" store).

### `Data`

The root for all framework data (catch-all for state that does not have a more specific kind).

### `GlobalSkills`

The shared, user-profile-level skill root (e.g. `~/.agent/skills`).
Skills installed here are visible to every host instance run by the user.

### `Templates`

The directory holding bundled and operator-supplied prompt/context templates.

### `Workspace`

The shared workspace directory, including templates and per-agent workspace overlays.

