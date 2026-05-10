# LlamaShears.Core.Abstractions.Paths.FileType

Assembly: `LlamaShears.Core.Abstractions`

Filesystem entry kind for path protection rules. [FileType](FileType.md).`Special`
covers sockets, FIFOs, devices, and other non-regular non-directory
entries. Reserved for future filtering; defaults treat them as
non-matching for file-only rules.

## Fields

### `Any`

Combined match for every concrete kind: [FileType](FileType.md).`File`, [FileType](FileType.md).`Directory`, and [FileType](FileType.md).`Special`.

### `Directory`

Directory entry.

### `File`

Regular file.

### `None`

No filesystem kind — sentinel for "rule does not apply".

### `Special`

Sockets, FIFOs, devices, and other non-regular non-directory entries.

