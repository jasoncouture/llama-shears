# LlamaShears.Core.Abstractions.Agent.Sessions

## Types

- [IAgentData](IAgentData.md) — Marker for any value that contributes one or more entries to an agent's per-turn data scope. Consumers (e.g. `IAgentFactory`) call [IAgentData](IAgentData.md).`GetData` and overlay the entries onto the scope's keyed dictionary.
- [ISessionFactory](ISessionFactory.md) — Per-agent registry of live sessions. Backed by a concurrent dictionary keyed by [SessionId](SessionId.md); sessions are created on first [ISessionFactory](ISessionFactory.md).`Get` via `ActivatorUtilities` and reused on subsequent requests.
- [ISessionQueue](ISessionQueue.md) — Per-session inbound queue for turns the model still needs to see. Carries two kinds of inputs — user messages arriving from channels, and tool-result turns produced by dispatched tool calls — and returns them to the run loop in the order strict providers require: any pending tool turns first, followed by an optional same-channel user batch.
- [SessionDataExtensions](SessionDataExtensions.md) — Extensions that overlay an [IAgentData](IAgentData.md)'s entries onto a target dictionary.
- [SessionExtensions](SessionExtensions.md) — Convenience accessors for pulling the active [SessionPath](SessionPath.md) off an [IDataContextScope](../../Common/IDataContextScope.md) without callers having to remember the well-known key.
- [SessionId](SessionId.md) — Identifier for a session: `agentId:defaultChannel` in canonical string form. Mirrors `EventType`'s shape — first segment is the agent id, second segment is the default channel for the session, and any further `:`-separated suffixes are absorbed into the channel part as opaque content (so channel ids that themselves carry colons, e.g. `telegram:123456`, round-trip cleanly).
- [SessionPath](SessionPath.md) — Parent/root chain for an agent session. [SessionPath](SessionPath.md).`Current` identifies this session; [SessionPath](SessionPath.md).`Parent` and [SessionPath](SessionPath.md).`Root` identify the ancestor in the session tree. For a root session all three refer to the same [SessionId](SessionId.md).

