namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Host-owned, process-wide slice of the agent context. Filled by a
/// system context provider that the host registers; agents do not own
/// or mutate this slice.
/// </summary>
public sealed record SystemContext(DateTimeOffset Now);
