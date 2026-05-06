# LlamaShears.Core.Abstractions.Provider.ThinkLevel

Assembly: `LlamaShears.Core.Abstractions.Provider`

Hint to a thinking-capable provider for how aggressively the model
should reason before answering. Providers that do not support
configurable thinking ignore non-[ThinkLevel](ThinkLevel.md).`None` values.

## Fields

### `High`

Most expensive thinking effort.

### `Low`

Cheapest non-zero thinking effort.

### `Medium`

Default thinking effort.

### `None`

Thinking is disabled (or unsupported by the model).

