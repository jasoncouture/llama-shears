using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.IntegrationTests.Hosting;

internal sealed class StubEmbeddingProviderFactory : IEmbeddingProviderFactory
{
    public string Name => "STUB";

    public async IAsyncEnumerable<ModelInfo> ListModelsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }

    public ValueTask<ValidationResult?> ValidateAsync(ModelConfiguration configuration, CancellationToken cancellationToken)
        => ValueTask.FromResult<ValidationResult?>(ValidationResult.Success);

    public IEmbeddingModel CreateModel(ModelConfiguration configuration) => new StubEmbeddingModel();
}
