using LlamaShears.Core.Abstractions.Provider;

using LlamaShears.Core.Abstractions.Common;
namespace LlamaShears.Core.Memory;

public sealed class MemoryServiceOptions
{
    public CompositeIdentity? DefaultEmbeddingModel { get; set; }
    public TimeSpan? DefaultEmbeddingKeepAlive { get; set; }
    public string? DefaultEmbeddingQueryPrefix { get; set; }
    public string? DefaultEmbeddingDocumentPrefix { get; set; }
    public MemoryIndexerOptions Indexer { get; set; } = new MemoryIndexerOptions();
}
