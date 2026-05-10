# LlamaShears.Core.Abstractions.Paths.IFileProtectionPolicy

Assembly: `LlamaShears.Core.Abstractions`

Decides whether a workspace-relative path is protected from a
requested operation. Implementations evaluate the registered set
of [ProtectedFile](ProtectedFile.md) rules.

## Methods

### `Match`(string workspaceRoot, string fullPath, [FileType](FileType.md) actualType, [ProtectionMode](ProtectionMode.md) requestedMode)

Returns the first [ProtectedFile](ProtectedFile.md) rule whose glob
(anchored at `workspaceRoot`) matches
`fullPath`, whose [FileType](FileType.md) covers
`actualType`, and whose [ProtectionMode](ProtectionMode.md)
includes `requestedMode`; returns `null`
when no rule applies.

#### Parameters

- `workspaceRoot` — Absolute path to the workspace root.
- `fullPath` — Absolute path of the entry under consideration. Paths inside
`workspaceRoot` are matched relative to it; paths
outside are matched against the absolute path so policies can also
protect system locations. Callers should resolve via
[IPathExpander](IPathExpander.md) before calling.
- `actualType` — Entry kind of the path on disk, or the kind that would be created
by a write/append.
- `requestedMode` — Mode the caller is about to perform.

