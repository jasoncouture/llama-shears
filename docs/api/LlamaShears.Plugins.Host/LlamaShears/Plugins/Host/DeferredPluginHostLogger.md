# LlamaShears.Plugins.Host.DeferredPluginHostLogger

Assembly: `LlamaShears.Plugins.Host`

Buffers plugin-host log calls into an in-memory queue so they can
be handed off to a real logger once one exists (typically after
DI is built). Entries are recorded in arrival order; concurrent
callers serialize on a monitor lock.

## Methods

### `Drain`

Atomically returns and clears the buffered entries in arrival
order. The caller is responsible for forwarding them to whatever
real logging stack is now available.

