# LlamaShears.Core.Abstractions.Agent.Sessions.EphemeralRunResult

Assembly: `LlamaShears.Core.Abstractions`

Outcome of an ephemeral session's `RunAsync` call.

## Parameters

- `ReplySent` — `true` when at least one reply was delivered to the
parent session — either by the session calling `session_reply`
at least once, or by the fallback path emitting the last assistant
content turn on its behalf.
- `UsedFallback` — `true` when the reply was produced by the fallback
path because `session_reply` was never called during the session.
Mutually exclusive with the explicit-tool path.
- `Iterations` — Number of iteration cycles the session loop completed before exiting.

## Properties

### `Iterations`

Number of iteration cycles the session loop completed before exiting.

### `ReplySent`

`true` when at least one reply was delivered to the
parent session — either by the session calling `session_reply`
at least once, or by the fallback path emitting the last assistant
content turn on its behalf.

### `UsedFallback`

`true` when the reply was produced by the fallback
path because `session_reply` was never called during the session.
Mutually exclusive with the explicit-tool path.

## Methods

### `EphemeralRunResult`(bool ReplySent, bool UsedFallback, int Iterations)

Outcome of an ephemeral session's `RunAsync` call.

#### Parameters

- `ReplySent` — `true` when at least one reply was delivered to the
parent session — either by the session calling `session_reply`
at least once, or by the fallback path emitting the last assistant
content turn on its behalf.
- `UsedFallback` — `true` when the reply was produced by the fallback
path because `session_reply` was never called during the session.
Mutually exclusive with the explicit-tool path.
- `Iterations` — Number of iteration cycles the session loop completed before exiting.

