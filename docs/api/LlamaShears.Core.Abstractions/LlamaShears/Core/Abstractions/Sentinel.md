# LlamaShears.Core.Abstractions.Sentinel

Assembly: `LlamaShears.Core.Abstractions`

Well-known sentinel strings used to signal special conditions in
model output that wouldn't survive cleanly as a typed value.

## Fields

### `NoResponse`

Emitted by the model when it has nothing to say. The inference
runner treats this as "suppress the turn" rather than forwarding
the literal text on to channels.

