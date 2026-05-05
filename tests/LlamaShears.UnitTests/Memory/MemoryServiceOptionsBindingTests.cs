using LlamaShears.Core.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LlamaShears.UnitTests.Memory;

public sealed class MemoryServiceOptionsBindingTests
{
    [Test]
    public async Task DefaultEmbeddingModelBindsFromProviderSlashModelString()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memory:DefaultEmbeddingModel"] = "OLLAMA/embeddinggemma:latest",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddOptions<MemoryServiceOptions>().Bind(configuration.GetSection("Memory"));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MemoryServiceOptions>>().Value;

        await Assert.That(options.DefaultEmbeddingModel).IsNotNull();
        await Assert.That(options.DefaultEmbeddingModel!.Provider).IsEqualTo("OLLAMA");
        await Assert.That(options.DefaultEmbeddingModel.Model).IsEqualTo("embeddinggemma:latest");
    }
}
