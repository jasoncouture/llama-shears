# LlamaShears.Core.Abstractions.Agent.Sessions.SessionId

Assembly: `LlamaShears.Core.Abstractions`

Identifier for a session: `agentId:defaultChannel` in canonical
string form. Mirrors `EventType`'s shape — first segment is the
agent id, second segment is the default channel for the session, and
any further `:`-separated suffixes are absorbed into the channel
part as opaque content (so channel ids that themselves carry colons,
e.g. `telegram:123456`, round-trip cleanly).

## Parameters

- `AgentId` — Agent the session belongs to.
- `DefaultChannel` — Channel the session is bound to. May contain `:` — everything after the first `:` in the canonical form is treated as part of this value.

## Properties

### `AgentId`

Agent the session belongs to.

### `DefaultChannel`

Channel the session is bound to. May contain `:` — everything after the first `:` in the canonical form is treated as part of this value.

## Methods

### `SessionId`(string AgentId, string DefaultChannel)

Identifier for a session: `agentId:defaultChannel` in canonical
string form. Mirrors `EventType`'s shape — first segment is the
agent id, second segment is the default channel for the session, and
any further `:`-separated suffixes are absorbed into the channel
part as opaque content (so channel ids that themselves carry colons,
e.g. `telegram:123456`, round-trip cleanly).

#### Parameters

- `AgentId` — Agent the session belongs to.
- `DefaultChannel` — Channel the session is bound to. May contain `:` — everything after the first `:` in the canonical form is treated as part of this value.

### `ToString`

### `TryParse`(string sessionId, [SessionId](SessionId.md)& typed)

Attempts to parse `sessionId`. Returns
`true` and assigns `typed` on
success; otherwise `false` with
`typed` set to `null`.

### `op_Explicit`(SessionId value)

Parses `value` in the canonical string form.
Throws ArgumentException when the input is not a
valid session id — use [SessionId](SessionId.md).`TryParse` for non-throwing
parsing.

### `op_Implicit`(String value)

Implicit conversion to the canonical string form (or `null` for a null receiver).

