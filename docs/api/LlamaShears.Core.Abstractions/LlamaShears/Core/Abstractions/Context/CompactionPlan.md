# LlamaShears.Core.Abstractions.Context.CompactionPlan

Assembly: `LlamaShears.Core.Abstractions`

Prepared compaction split: the transcript the summarizer should see,
plus the live turns that survive after the summary lands.

## Parameters

- `SystemTurn` — The original (non-compaction) system turn from the live prompt, if
any. Reattached when the rebuilt prompt is committed.
- `Transcript` — Flattened older history as a single user turn.
- `Preserved` — Eligible turns kept after the summary.

## Properties

### `Preserved`

Eligible turns kept after the summary.

### `SystemTurn`

The original (non-compaction) system turn from the live prompt, if
any. Reattached when the rebuilt prompt is committed.

### `Transcript`

Flattened older history as a single user turn.

## Methods

### `CompactionPlan`([ModelTurn](../Provider/ModelTurn.md) SystemTurn, [ModelTurn](../Provider/ModelTurn.md) Transcript, ImmutableArray<[ModelTurn](../Provider/ModelTurn.md)> Preserved)

Prepared compaction split: the transcript the summarizer should see,
plus the live turns that survive after the summary lands.

#### Parameters

- `SystemTurn` — The original (non-compaction) system turn from the live prompt, if
any. Reattached when the rebuilt prompt is committed.
- `Transcript` — Flattened older history as a single user turn.
- `Preserved` — Eligible turns kept after the summary.

