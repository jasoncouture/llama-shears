namespace LlamaShears.Agent.Abstractions.Persistence;

public readonly record struct ArchiveId(string AgentId, long UnixMillis);
