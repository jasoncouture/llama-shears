# LlamaShears.Core.Abstractions.Agent.SystemTick

Assembly: `LlamaShears.Core.Abstractions`

Periodic host heartbeat broadcast onto the event bus. Subscribers
use it as a coarse "wall-clock advanced" signal — agent idle
detection, refreshes, scheduled chores — without each component
running its own timer.

## Parameters

- `At` — Wall-clock time the tick was emitted.

## Properties

### `At`

Wall-clock time the tick was emitted.

## Methods

### `SystemTick`(DateTimeOffset At)

Periodic host heartbeat broadcast onto the event bus. Subscribers
use it as a coarse "wall-clock advanced" signal — agent idle
detection, refreshes, scheduled chores — without each component
running its own timer.

#### Parameters

- `At` — Wall-clock time the tick was emitted.

