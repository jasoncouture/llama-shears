# LlamaShears.Core.Abstractions.Paths.ProtectedFile

Assembly: `LlamaShears.Core.Abstractions`

Declares a protection rule for paths inside an agent workspace.

## Parameters

- `Glob` — FileSystemGlobbing-style glob anchored at the workspace root
(e.g. `.git/**`, `*.md`, `**/.git`). Matched
case-insensitively against forward-slash relative paths.
- `ProtectionMode` — Modes denied for matched paths.
- `Type` — Filesystem entry kinds the rule applies to.
- `Reason` — Optional explanation surfaced in refusal messages.

## Properties

### `Glob`

FileSystemGlobbing-style glob anchored at the workspace root
(e.g. `.git/**`, `*.md`, `**/.git`). Matched
case-insensitively against forward-slash relative paths.

### `ProtectionMode`

Modes denied for matched paths.

### `Reason`

Optional explanation surfaced in refusal messages.

### `Type`

Filesystem entry kinds the rule applies to.

## Methods

### `ProtectedFile`(string Glob, [ProtectionMode](ProtectionMode.md) ProtectionMode, [FileType](FileType.md) Type, string Reason)

Declares a protection rule for paths inside an agent workspace.

#### Parameters

- `Glob` — FileSystemGlobbing-style glob anchored at the workspace root
(e.g. `.git/**`, `*.md`, `**/.git`). Matched
case-insensitively against forward-slash relative paths.
- `ProtectionMode` — Modes denied for matched paths.
- `Type` — Filesystem entry kinds the rule applies to.
- `Reason` — Optional explanation surfaced in refusal messages.

