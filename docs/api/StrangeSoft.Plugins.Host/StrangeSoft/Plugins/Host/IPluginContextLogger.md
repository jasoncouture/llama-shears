# StrangeSoft.Plugins.Host.IPluginContextLogger

Assembly: `StrangeSoft.Plugins.Host`

Notification sink for events that happen inside the plugin loader
— host graph walk, plugin loader instantiation, plugin loader
invocation. The loader prefers to swallow per-item failures so a
single bad assembly or loader doesn't take everything down; this
interface gives the host a place to surface those failures (or
not) in whatever logging stack it owns.

## Remarks

Format strings follow the .NET `ILogger` message-template
convention (named placeholders like `{AssemblyName}`); it is
the implementation's responsibility to wire that through to its
underlying logging stack.

## Methods

### `Debug`(string format, IEnumerable<object> data)

Records a diagnostic-level message — verbose detail useful when
inspecting loader behavior, normally off in production.

#### Parameters

- `format` — Message template (named placeholders).
- `data` — Values for the placeholders, in order.

### `Error`(string format, Exception exception, IEnumerable<object> data)

Records a failure — something the loader couldn't recover from
at this granularity (e.g. a plugin loader's `LoadAsync`
threw). Pass the originating exception when one is available.

#### Parameters

- `format` — Message template (named placeholders).
- `exception` — The exception associated with the error, or `null`.
- `data` — Values for the placeholders, in order.

### `Information`(string format, IEnumerable<object> data)

Records an informational message — routine progress about the
loader's work that the host may want to surface.

#### Parameters

- `format` — Message template (named placeholders).
- `data` — Values for the placeholders, in order.

### `Warning`(string format, Exception exception, IEnumerable<object> data)

Records a non-fatal problem — the loader recovered (e.g. by
skipping a bad assembly or loader type), but the host may want
to know. Pass the originating exception when one is available.

#### Parameters

- `format` — Message template (named placeholders).
- `exception` — The exception associated with the warning, or `null`.
- `data` — Values for the placeholders, in order.

