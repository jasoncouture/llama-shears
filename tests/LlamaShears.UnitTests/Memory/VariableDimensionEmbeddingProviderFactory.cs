using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Memory;

// Stub embedding provider whose vector dimension is mutable at runtime,
// so a single test can index at one dim and then reconcile at another to
// exercise the schema-mismatch recovery path in SqliteMemoryService.
internal sealed class VariableDimensionEmbeddingProviderFactory : IEmbeddingProviderFactory
{
    private int _dimensions;

    public VariableDimensionEmbeddingProviderFactory(int dimensions)
    {
        _dimensions = dimensions;
    }

    public string Name => "STUB";

    public int Dimensions
    {
        get => _dimensions;
        set => _dimensions = value;
    }

    public async IAsyncEnumerable<ModelInfo> ListModelsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }

    public IEmbeddingModel CreateModel(ModelConfiguration configuration) =>
        new VariableDimensionEmbeddingModel(() => _dimensions);
}
