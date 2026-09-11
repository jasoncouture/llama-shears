# LlamaShears.Core.Abstractions.Agent.SubagentRunResult

Assembly: `LlamaShears.Core.Abstractions`

Outcome of [ISubagentRunner](ISubagentRunner.md).`RunAsync`.
[SubagentRunResult](SubagentRunResult.md).`Ok` means the child finished while the caller
waited. [SubagentRunResult](SubagentRunResult.md).`Error` is refuse / spawn / timeout
transport — not the child's HTTP-style status.

## Parameters

- `Ok` — `true` when the awaited child reached idle.
- `SessionId` — Canonical child session id, when a session was created.
- `Awaited` — `true` when the caller asked to wait.
- `Output` — Last assistant text captured from the child, if any.
- `TimedOut` — `true` when the await ceiling elapsed.
- `Error` — Refuse, spawn, or timeout explanation; `null` on success.
- `Started` — `true` when the child session was spawned.

## Properties

### `Awaited`

`true` when the caller asked to wait.

### `Error`

Refuse, spawn, or timeout explanation; `null` on success.

### `Ok`

`true` when the awaited child reached idle.

### `Output`

Last assistant text captured from the child, if any.

### `SessionId`

Canonical child session id, when a session was created.

### `Started`

`true` when the child session was spawned.

### `TimedOut`

`true` when the await ceiling elapsed.

## Methods

### `SubagentRunResult`(bool Ok, string SessionId, bool Awaited, string Output, bool TimedOut, string Error, bool Started)

Outcome of [ISubagentRunner](ISubagentRunner.md).`RunAsync`.
[SubagentRunResult](SubagentRunResult.md).`Ok` means the child finished while the caller
waited. [SubagentRunResult](SubagentRunResult.md).`Error` is refuse / spawn / timeout
transport — not the child's HTTP-style status.

#### Parameters

- `Ok` — `true` when the awaited child reached idle.
- `SessionId` — Canonical child session id, when a session was created.
- `Awaited` — `true` when the caller asked to wait.
- `Output` — Last assistant text captured from the child, if any.
- `TimedOut` — `true` when the await ceiling elapsed.
- `Error` — Refuse, spawn, or timeout explanation; `null` on success.
- `Started` — `true` when the child session was spawned.

