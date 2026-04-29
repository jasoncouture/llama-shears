namespace LlamaShears.Agent.Core;

/// <summary>
/// Global toggle for the <see cref="AgentHeartbeatService"/>. Per-agent
/// cadence lives on <see cref="LlamaShears.Agent.Abstractions.IAgent"/>
/// itself.
/// </summary>
public sealed class AgentHeartbeatOptions
{
    /// <summary>
    /// When <see langword="false"/>, the heartbeat service skips all
    /// agents on every tick.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
