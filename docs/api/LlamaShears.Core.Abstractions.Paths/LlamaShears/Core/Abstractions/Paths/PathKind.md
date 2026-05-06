# LlamaShears.Core.Abstractions.Paths.PathKind

Assembly: `LlamaShears.Core.Abstractions.Paths`

Well-known categories of host state whose on-disk root is
resolved by [IShearsPaths](IShearsPaths.md). Implementations decide
where each root lives and whether to create directories on demand.

## Fields

### `Agents`

The directory holding per-agent `<id>.json` configuration files.

### `Context`

The directory holding per-agent persisted conversation logs (the "context" store).

### `Data`

The root for all framework data (catch-all for state that does not have a more specific kind).

### `Templates`

The directory holding bundled and operator-supplied prompt/context templates.

### `Workspace`

The shared workspace directory, including templates and per-agent workspace overlays.

