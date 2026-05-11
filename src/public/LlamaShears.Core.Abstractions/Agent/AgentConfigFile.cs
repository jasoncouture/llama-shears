namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Raw on-disk snapshot of an agent's config file: the verbatim JSON text
/// alongside the SHA-256 hash of the file bytes at read time. Used by the
/// editor UI as the change token for optimistic-concurrency saves.
/// </summary>
/// <param name="Content">UTF-8 text content of the file.</param>
/// <param name="Hash">SHA-256 hex digest of the file bytes.</param>
public sealed record AgentConfigFile(string Content, string Hash);
