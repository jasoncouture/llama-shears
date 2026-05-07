namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Per-agent memory-subsystem options.
/// </summary>
/// <param name="Prefetch">
/// When <see langword="true"/>, the agent kicks off the per-batch memory
/// search the moment an inbound <c>ChannelMessage</c> lands at its event
/// handler — concurrently with whatever the agent is doing right then —
/// instead of waiting until the batch reaches the search step. The win is
/// overlap: embedding-model latency hides behind work the agent was doing
/// anyway. Falls back to a synchronous search if the prefetch slot is
/// missing on a given batch.
/// </param>
public sealed record AgentMemoryConfig(bool Prefetch = false);
