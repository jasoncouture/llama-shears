# LlamaShears.Core.Abstractions.Events.Agent.AgentCompactionRequest

Assembly: `LlamaShears.Core.Abstractions`

Payload for [Command](../Event/WellKnown/Command.md).`CompactionRequest`
and the start/finish events around a compaction pass. [AgentCompactionRequest](AgentCompactionRequest.md).`Force`
tells the compactor to bypass its usual under-budget guard.

## Parameters

- `Force` — When `true`, the compactor bypasses its under-budget guard and runs anyway; the other guards (min-turn-count, missing context length) still apply.

## Properties

### `Force`

When `true`, the compactor bypasses its under-budget guard and runs anyway; the other guards (min-turn-count, missing context length) still apply.

### `Forced`

Forces a compaction pass regardless of budget pressure.

### `Normal`

Lets the compactor decide whether compaction is needed.

## Methods

### `AgentCompactionRequest`(bool Force)

Payload for [Command](../Event/WellKnown/Command.md).`CompactionRequest`
and the start/finish events around a compaction pass. [AgentCompactionRequest](AgentCompactionRequest.md).`Force`
tells the compactor to bypass its usual under-budget guard.

#### Parameters

- `Force` — When `true`, the compactor bypasses its under-budget guard and runs anyway; the other guards (min-turn-count, missing context length) still apply.

