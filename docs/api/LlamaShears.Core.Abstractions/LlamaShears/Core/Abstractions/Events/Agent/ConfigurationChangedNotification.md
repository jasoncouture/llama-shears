# LlamaShears.Core.Abstractions.Events.Agent.ConfigurationChangedNotification

Assembly: `LlamaShears.Core.Abstractions`

Carries both ends of a config diff so subscribers can decide between birth, tombstone,
and update cases.

## Parameters

- `CurrentConfig` — Last-known config before the change; `null` when this is a birth.
- `UpdatedConfig` — New config after the change; `null` when this is a tombstone.

## Properties

### `CurrentConfig`

Last-known config before the change; `null` when this is a birth.

### `HasChanges`

`true` when this notification represents an actual change (birth, tombstone, or hash-distinct update).

### `IsBirth`

`true` when a config has appeared for the first time.

### `IsTombstone`

`true` when a previously-known config has been removed.

### `IsUpdate`

`true` when both old and new configs are present (i.e. a mutation).

### `Name`

Agent id, taken from whichever config is non-null.

### `UpdatedConfig`

New config after the change; `null` when this is a tombstone.

## Methods

### `ConfigurationChangedNotification`([AgentConfig](../../Agent/AgentConfig.md) CurrentConfig, [AgentConfig](../../Agent/AgentConfig.md) UpdatedConfig)

Carries both ends of a config diff so subscribers can decide between birth, tombstone,
and update cases.

#### Parameters

- `CurrentConfig` — Last-known config before the change; `null` when this is a birth.
- `UpdatedConfig` — New config after the change; `null` when this is a tombstone.

