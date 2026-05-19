# LlamaShears.Core.Abstractions.Agent.Sessions.EphemeralSessionContext

Assembly: `LlamaShears.Core.Abstractions`

Live state for an ephemeral session, stashed in the session's data
context scope under [EphemeralSessionContext](EphemeralSessionContext.md).`DataKey`. The session's
`session_reply` tool reads [EphemeralSessionContext](EphemeralSessionContext.md).`Parent` /
[EphemeralSessionContext](EphemeralSessionContext.md).`ChannelId` / [EphemeralSessionContext](EphemeralSessionContext.md).`SessionId` to publish back to
the parent and flips [EphemeralSessionContext](EphemeralSessionContext.md).`ReplySent` so the owning session
knows the fallback path is not needed.

## Fields

### `DataKey`

Data-context key for the active [EphemeralSessionContext](EphemeralSessionContext.md).

## Properties

### `ChannelId`

Effective channel id used for ModelTurn tagging and the session_reply event suffix.

### `Parent`

Parent session this ephemeral child sends its reply to.

### `ReplySent`

Set by the `session_reply` tool when it successfully
publishes. Read by the owning `IEphemeralSession` after the
loop exits to decide whether the fallback path should fire.

### `SessionId`

This ephemeral session's own id; stamped onto outbound payloads.

