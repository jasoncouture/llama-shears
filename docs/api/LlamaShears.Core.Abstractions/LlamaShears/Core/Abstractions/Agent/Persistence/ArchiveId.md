# LlamaShears.Core.Abstractions.Agent.Persistence.ArchiveId

Assembly: `LlamaShears.Core.Abstractions`

Identifies a single archived (rotated-out) context file. The tuple
([ArchiveId](ArchiveId.md).`Session`, [ArchiveId](ArchiveId.md).`UnixMillis`) uniquely names
`<agentId>/<UnixMillis>.json` (default session) or
`<agentId>/<sessionName>/<UnixMillis>.json`
(non-default session) in the context store.

## Parameters

- `Session` — Session the archive belongs to.
- `UnixMillis` — Archive timestamp, milliseconds since the Unix epoch.

## Properties

### `Session`

Session the archive belongs to.

### `UnixMillis`

Archive timestamp, milliseconds since the Unix epoch.

## Methods

### `ArchiveId`([SessionId](../Sessions/SessionId.md) Session, long UnixMillis)

Identifies a single archived (rotated-out) context file. The tuple
([ArchiveId](ArchiveId.md).`Session`, [ArchiveId](ArchiveId.md).`UnixMillis`) uniquely names
`<agentId>/<UnixMillis>.json` (default session) or
`<agentId>/<sessionName>/<UnixMillis>.json`
(non-default session) in the context store.

#### Parameters

- `Session` — Session the archive belongs to.
- `UnixMillis` — Archive timestamp, milliseconds since the Unix epoch.

