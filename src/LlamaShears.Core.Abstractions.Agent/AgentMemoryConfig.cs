namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Per-agent memory-subsystem options.
/// </summary>
/// <param name="Prefetch">When <see langword="true"/>, the agent eagerly searches memory before each turn rather than waiting for the model to call the search tool.</param>
public sealed record AgentMemoryConfig(bool Prefetch = false);
