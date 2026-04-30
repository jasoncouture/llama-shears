namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Host-owned, process-wide slice of the agent context. Filled by a
/// system context provider that the host registers; agents do not own
/// or mutate this slice. Empty for now — fields will be added as
/// host-level state earns its place in the snapshot.
/// </summary>
public sealed record SystemContext;
