namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Per-agent tool-loop guardrails.
/// </summary>
public sealed record AgentToolConfig
{
    /// <summary>
    /// Maximum model rounds one inbound user or framework-user
    /// batch may drive. Zero (the default) is unlimited. A
    /// positive <c>N</c> runs iterations 1..<c>N-1</c> with the
    /// full tool catalog and iteration <c>N</c> with none; leftover
    /// tool calls on that last round are dropped so the queue
    /// can drain. This default stays unlimited so existing agents
    /// are not silently capped.
    /// </summary>
    public int TurnLimit { get; init; }
}
