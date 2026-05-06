namespace LlamaShears.Core.Abstractions.Agent.Persistence;

/// <summary>
/// Identifies a single archived (rotated-out) context file for an
/// agent. The pair (<see cref="AgentId"/>, <see cref="UnixMillis"/>)
/// uniquely names <c>&lt;AgentId&gt;/&lt;UnixMillis&gt;.json</c> in the
/// context store.
/// </summary>
/// <param name="AgentId">Agent the archive belongs to.</param>
/// <param name="UnixMillis">Archive timestamp, milliseconds since the Unix epoch.</param>
public readonly record struct ArchiveId(string AgentId, long UnixMillis);
