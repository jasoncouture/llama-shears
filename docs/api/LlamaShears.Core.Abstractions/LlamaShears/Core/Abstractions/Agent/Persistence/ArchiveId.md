# LlamaShears.Core.Abstractions.Agent.Persistence.ArchiveId

Assembly: `LlamaShears.Core.Abstractions`

Identifies a single archived (rotated-out) context file for an
agent. The pair ([ArchiveId](ArchiveId.md).`AgentId`, [ArchiveId](ArchiveId.md).`UnixMillis`)
uniquely names `<AgentId>/<UnixMillis>.json` in the
context store.

## Parameters

- `AgentId` — Agent the archive belongs to.
- `UnixMillis` — Archive timestamp, milliseconds since the Unix epoch.

## Properties

### `AgentId`

Agent the archive belongs to.

### `UnixMillis`

Archive timestamp, milliseconds since the Unix epoch.

## Methods

### `ArchiveId`(string AgentId, long UnixMillis)

Identifies a single archived (rotated-out) context file for an
agent. The pair ([ArchiveId](ArchiveId.md).`AgentId`, [ArchiveId](ArchiveId.md).`UnixMillis`)
uniquely names `<AgentId>/<UnixMillis>.json` in the
context store.

#### Parameters

- `AgentId` — Agent the archive belongs to.
- `UnixMillis` — Archive timestamp, milliseconds since the Unix epoch.

