# LlamaShears.Core.Abstractions.Agent.AgentConfigFile

Assembly: `LlamaShears.Core.Abstractions`

Raw on-disk snapshot of an agent's config file: the verbatim JSON text
alongside the SHA-256 hash of the file bytes at read time. Used by the
editor UI as the change token for optimistic-concurrency saves.

## Parameters

- `Content` — UTF-8 text content of the file.
- `Hash` — SHA-256 hex digest of the file bytes.

## Properties

### `Content`

UTF-8 text content of the file.

### `Hash`

SHA-256 hex digest of the file bytes.

## Methods

### `AgentConfigFile`(string Content, string Hash)

Raw on-disk snapshot of an agent's config file: the verbatim JSON text
alongside the SHA-256 hash of the file bytes at read time. Used by the
editor UI as the change token for optimistic-concurrency saves.

#### Parameters

- `Content` — UTF-8 text content of the file.
- `Hash` — SHA-256 hex digest of the file bytes.

