namespace LlamaShears.Core.Abstractions.Agent.Persistence;

public readonly record struct ArchiveId(string AgentId, long UnixMillis);
