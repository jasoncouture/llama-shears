# LlamaShears.Core.Abstractions.Agent.Persistence.ArchiveId

Assembly: `LlamaShears.Core.Abstractions`

Identifies a single archived (rotated-out) context file. The tuple
([ArchiveId](ArchiveId.md).`AgentId`, [ArchiveId](ArchiveId.md).`SessionId`, [ArchiveId](ArchiveId.md).`UnixMillis`)
uniquely names `<AgentId>/<UnixMillis>.json` (default
session, when [ArchiveId](ArchiveId.md).`SessionId` is Guid.`Empty`) or
`<AgentId>/<sessionGuid>/<UnixMillis>.json`
(non-default session) in the context store.

## Parameters

- `AgentId` — Agent the archive belongs to.
- `SessionId` — Session the archive belongs to; Guid.`Empty` = the agent's
default (main) session and resolves to the agent root layout.
- `UnixMillis` — Archive timestamp, milliseconds since the Unix epoch.

## Properties

### `AgentId`

Agent the archive belongs to.

### `SessionId`

Session the archive belongs to; Guid.`Empty` = the agent's
default (main) session and resolves to the agent root layout.

### `UnixMillis`

Archive timestamp, milliseconds since the Unix epoch.

## Methods

### `ArchiveId`(string AgentId, Guid SessionId, long UnixMillis)

Identifies a single archived (rotated-out) context file. The tuple
([ArchiveId](ArchiveId.md).`AgentId`, [ArchiveId](ArchiveId.md).`SessionId`, [ArchiveId](ArchiveId.md).`UnixMillis`)
uniquely names `<AgentId>/<UnixMillis>.json` (default
session, when [ArchiveId](ArchiveId.md).`SessionId` is Guid.`Empty`) or
`<AgentId>/<sessionGuid>/<UnixMillis>.json`
(non-default session) in the context store.

#### Parameters

- `AgentId` — Agent the archive belongs to.
- `SessionId` — Session the archive belongs to; Guid.`Empty` = the agent's
default (main) session and resolves to the agent root layout.
- `UnixMillis` — Archive timestamp, milliseconds since the Unix epoch.

