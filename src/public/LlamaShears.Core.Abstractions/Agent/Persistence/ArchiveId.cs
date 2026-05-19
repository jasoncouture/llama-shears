namespace LlamaShears.Core.Abstractions.Agent.Persistence;

/// <summary>
/// Identifies a single archived (rotated-out) context file. The tuple
/// (<see cref="AgentId"/>, <see cref="SessionId"/>, <see cref="UnixMillis"/>)
/// uniquely names <c>&lt;AgentId&gt;/&lt;UnixMillis&gt;.json</c> (default
/// session, when <see cref="SessionId"/> is <see langword="null"/>) or
/// <c>&lt;AgentId&gt;/&lt;sessionGuid&gt;/&lt;UnixMillis&gt;.json</c>
/// (non-default session) in the context store.
/// </summary>
/// <param name="AgentId">Agent the archive belongs to.</param>
/// <param name="SessionId">
/// Session the archive belongs to; <see langword="null"/> = the agent's
/// default (main) session and resolves to the agent root layout.
/// </param>
/// <param name="UnixMillis">Archive timestamp, milliseconds since the Unix epoch.</param>
public readonly record struct ArchiveId(string AgentId, Guid? SessionId, long UnixMillis);
