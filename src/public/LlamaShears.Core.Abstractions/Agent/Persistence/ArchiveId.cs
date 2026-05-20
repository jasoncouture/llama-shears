using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Core.Abstractions.Agent.Persistence;

/// <summary>
/// Identifies a single archived (rotated-out) context file. The tuple
/// (<see cref="Session"/>, <see cref="UnixMillis"/>) uniquely names
/// <c>&lt;agentId&gt;/&lt;UnixMillis&gt;.json</c> (default session) or
/// <c>&lt;agentId&gt;/&lt;sessionName&gt;/&lt;UnixMillis&gt;.json</c>
/// (non-default session) in the context store.
/// </summary>
/// <param name="Session">Session the archive belongs to.</param>
/// <param name="UnixMillis">Archive timestamp, milliseconds since the Unix epoch.</param>
public readonly record struct ArchiveId(SessionId Session, long UnixMillis);
