namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Payload for <see cref="Event.WellKnown.Agent.CompactionRequested"/>
/// and the start/finish events around a compaction pass. <see cref="Force"/>
/// tells the compactor to bypass its usual under-budget guard.
/// </summary>
/// <param name="Force">When <see langword="true"/>, the compactor bypasses its under-budget guard and runs anyway; the other guards (min-turn-count, missing context length) still apply.</param>
public sealed record AgentCompactionRequest(bool Force = false)
{
    /// <summary>Lets the compactor decide whether compaction is needed.</summary>
    public static AgentCompactionRequest Normal { get; } = new AgentCompactionRequest();

    /// <summary>Forces a compaction pass regardless of budget pressure.</summary>
    public static AgentCompactionRequest Forced { get; } = new AgentCompactionRequest(true);
}
