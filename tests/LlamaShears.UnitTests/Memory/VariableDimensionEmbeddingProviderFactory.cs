using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Memory;

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
        await Task.CompletedTask;
        yield break;
    }

    public ValueTask<ValidationResult?> ValidateAsync(ModelConfiguration configuration, CancellationToken cancellationToken)
        => ValueTask.FromResult(ValidationResult.Success);

    public IEmbeddingModel CreateModel(ModelConfiguration configuration) =>
        new VariableDimensionEmbeddingModel(() => _dimensions);
}
