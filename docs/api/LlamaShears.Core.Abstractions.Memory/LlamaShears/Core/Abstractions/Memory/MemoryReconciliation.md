# LlamaShears.Core.Abstractions.Memory.MemoryReconciliation

Assembly: `LlamaShears.Core.Abstractions.Memory`

Counts returned by [IMemoryIndexer](IMemoryIndexer.md).`ReconcileAsync`. Pure
telemetry — the source of truth for memory content is the
filesystem, the index is derivative.

## Parameters

- `Added` — Files newly indexed during this pass.
- `Updated` — Files re-indexed because their content hash changed (or `force` was set).
- `Removed` — Index entries deleted because the underlying file no longer exists.
- `Total` — Total files observed under the agent's `memory/` tree.

## Properties

### `Added`

Files newly indexed during this pass.

### `Removed`

Index entries deleted because the underlying file no longer exists.

### `Total`

Total files observed under the agent's `memory/` tree.

### `Updated`

Files re-indexed because their content hash changed (or `force` was set).

## Methods

### `MemoryReconciliation`(int Added, int Updated, int Removed, int Total)

Counts returned by [IMemoryIndexer](IMemoryIndexer.md).`ReconcileAsync`. Pure
telemetry — the source of truth for memory content is the
filesystem, the index is derivative.

#### Parameters

- `Added` — Files newly indexed during this pass.
- `Updated` — Files re-indexed because their content hash changed (or `force` was set).
- `Removed` — Index entries deleted because the underlying file no longer exists.
- `Total` — Total files observed under the agent's `memory/` tree.

