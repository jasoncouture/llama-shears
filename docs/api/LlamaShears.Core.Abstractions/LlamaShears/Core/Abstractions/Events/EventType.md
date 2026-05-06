# LlamaShears.Core.Abstractions.Events.EventType

Assembly: `LlamaShears.Core.Abstractions`

Hierarchical event identifier of the form
`component:eventName[:id]`. The optional third segment carries
a per-instance discriminator so subscribers can pattern-match on
the prefix while keeping correlation in the suffix.

## Parameters

- `Component` — Coarse source group, e.g. `"agent"` or `"system"`.
- `EventName` — Event name within `Component`.
- `Id` — Optional per-instance discriminator (e.g. agent id, channel id). `null` = no discriminator.

## Properties

### `Component`

Coarse source group, e.g. `"agent"` or `"system"`.

### `EventName`

Event name within `Component`.

### `Id`

Optional per-instance discriminator (e.g. agent id, channel id). `null` = no discriminator.

## Methods

### `EventType`(string Component, string EventName, string Id)

Hierarchical event identifier of the form
`component:eventName[:id]`. The optional third segment carries
a per-instance discriminator so subscribers can pattern-match on
the prefix while keeping correlation in the suffix.

#### Parameters

- `Component` — Coarse source group, e.g. `"agent"` or `"system"`.
- `EventName` — Event name within `Component`.
- `Id` — Optional per-instance discriminator (e.g. agent id, channel id). `null` = no discriminator.

### `ToString`

### `TryParse`(string eventType, [EventType](EventType.md)& typed)

Attempts to parse `eventType`. Returns
`true` and assigns `typed` on
success; otherwise `false` with
`typed` set to `null`.

### `op_Explicit`(EventType value)

Parses `value` in the canonical string form.
Throws ArgumentException when the input is not a
valid event type — use [EventType](EventType.md).`TryParse` for non-throwing
parsing.

### `op_Implicit`(String value)

Implicit conversion to the canonical string form (or `null` for a null receiver).

