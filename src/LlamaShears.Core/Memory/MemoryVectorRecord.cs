namespace LlamaShears.Core.Memory;

internal sealed class MemoryVectorRecord
{
    public required string Path { get; init; }
    public required string Hash { get; init; }
    public ReadOnlyMemory<float> Vector { get; init; }
}
