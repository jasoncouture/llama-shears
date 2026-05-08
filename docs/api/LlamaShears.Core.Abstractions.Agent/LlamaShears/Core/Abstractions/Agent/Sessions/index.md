# LlamaShears.Core.Abstractions.Agent.Sessions

## Types

- [ISessionFactory](ISessionFactory.md) — Per-agent registry of live sessions. Backed by a concurrent dictionary keyed by [SessionId](SessionId.md); sessions are created on first [ISessionFactory](ISessionFactory.md).`Get` via `ActivatorUtilities` and reused on subsequent requests.
- [ISessionQueue](ISessionQueue.md) — Per-session inbound queue for turns the model still needs to see. Carries two kinds of inputs — user messages arriving from channels, and tool-result turns produced by dispatched tool calls — and returns them to the run loop in the order strict providers require: any pending tool turns first, followed by an optional same-channel user batch.
- [SessionId](SessionId.md) — Identifier for a session: `agentId:defaultChannel` in canonical string form. Mirrors `EventType`'s shape — first segment is the agent id, second segment is the default channel for the session, and any further `:`-separated suffixes are absorbed into the channel part as opaque content (so channel ids that themselves carry colons, e.g. `telegram:123456`, round-trip cleanly).

