# LlamaShears.Core.Abstractions.Paths.IShearsPaths

Assembly: `LlamaShears.Core.Abstractions.Paths`

Resolves on-disk paths for the well-known categories of host state
([PathKind](PathKind.md)). Implementations decide where each root
lives and whether to create directories on demand.

## Methods

### `GetPath`([PathKind](PathKind.md) kind, string subpath, bool ensureExists)

Returns the absolute path for `kind`, optionally
joined with `subpath`. When
`ensureExists` is `true`, the
resulting directory is created if missing.

