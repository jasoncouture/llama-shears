using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.IntegrationTests.Hosting;

/// <summary>
/// In-memory <see cref="IProviderFactory"/> backing the test host.
/// Every <see cref="CreateModel"/> call returns the same
/// <see cref="StubLanguageModel"/> instance so tests can inspect
/// invocation counts without bookkeeping per agent.
/// </summary>
public sealed class StubProviderFactory : IProviderFactory
{
    public const string ProviderName = "TEST";

    public StubProviderFactory()
        : this(new StubLanguageModel())
    {
    }

    public StubProviderFactory(StubLanguageModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    public string Name => ProviderName;

    public StubLanguageModel Model { get; }

    public async IAsyncEnumerable<ModelInfo> ListModelsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    public ILanguageModel CreateModel(ModelConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return Model;
    }
}
