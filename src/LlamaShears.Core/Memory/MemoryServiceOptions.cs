using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Memory;

public sealed class MemoryServiceOptions
{
    public ModelIdentity? DefaultEmbeddingModel { get; set; }
    public TimeSpan? DefaultEmbeddingKeepAlive { get; set; }
}
