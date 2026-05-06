namespace LlamaShears.Core.Abstractions.Memory;

/// <summary>
/// Lightweight reference to a memory file written via
/// <see cref="IMemoryStore.StoreAsync"/>. Workspace-relative path only —
/// the agent reads the body on demand.
/// </summary>
/// <param name="RelativePath">Workspace-relative path to the memory file.</param>
public sealed record MemoryRef(string RelativePath);
