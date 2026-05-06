using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Provider.Ollama;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlamaShears.UnitTests;

public class OllamaProviderTests
{
    private static ServiceCollection ServicesWithConfig(IDictionary<string, string?>? values = null)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        return services;
    }

    [Test]
    public async Task AddOllamaProviderRegistersProviderFactory()
    {
        var services = ServicesWithConfig();

        services.AddOllamaProvider();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IProviderFactory>();

        await Assert.That(factory).IsTypeOf<OllamaProviderFactory>();
    }

    [Test]
    public async Task AddOllamaProviderRegistersOllamaApiClientFactory()
    {
        var services = ServicesWithConfig();

        services.AddOllamaProvider();

        using var provider = services.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IOllamaApiClientFactory>();

        await Assert.That(clientFactory).IsNotNull();
    }

    [Test]
    public async Task AddOllamaProviderBindsDefaultSection()
    {
        var services = ServicesWithConfig(new Dictionary<string, string?>
        {
            ["Providers:Ollama:BaseUri"] = "http://ollama.example:9999"
        });

        services.AddOllamaProvider();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OllamaProviderOptions>>().Value;

        await Assert.That(options.BaseUri).IsEqualTo(new Uri("http://ollama.example:9999"));
    }

    [Test]
    public async Task AddOllamaProviderBindsCustomSection()
    {
        var services = ServicesWithConfig(new Dictionary<string, string?>
        {
            ["Custom:Path:BaseUri"] = "http://ollama.custom:1234"
        });

        services.AddOllamaProvider("Custom:Path");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OllamaProviderOptions>>().Value;

        await Assert.That(options.BaseUri).IsEqualTo(new Uri("http://ollama.custom:1234"));
    }

    [Test]
    public async Task OllamaProviderOptionsDefaultBaseUriIsLocalhost11434()
    {
        var options = new OllamaProviderOptions();

        await Assert.That(options.BaseUri).IsEqualTo(new Uri("http://localhost:11434"));
    }

    [Test]
    public async Task FactoryNameIsOllama()
    {
        var services = ServicesWithConfig();
        services.AddOllamaProvider();
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IProviderFactory>();

        await Assert.That(factory.Name).IsEqualTo("ollama");
    }

    [Test]
    public async Task FactoryCreateModelReturnsOllamaLanguageModel()
    {
        var services = ServicesWithConfig();
        services.AddOllamaProvider();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IProviderFactory>();

        var model = factory.CreateModel(new ModelConfiguration("llama3"));

        await Assert.That(model).IsTypeOf<OllamaLanguageModel>();
    }

    [Test]
    public async Task AddOllamaProviderRegistersEmbeddingProviderFactory()
    {
        var services = ServicesWithConfig();

        services.AddOllamaProvider();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEmbeddingProviderFactory>();

        await Assert.That(factory).IsTypeOf<OllamaEmbeddingProviderFactory>();
    }

    [Test]
    public async Task EmbeddingFactoryNameIsOllama()
    {
        var services = ServicesWithConfig();
        services.AddOllamaProvider();
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IEmbeddingProviderFactory>();

        await Assert.That(factory.Name).IsEqualTo("ollama");
    }

    [Test]
    public async Task EmbeddingFactoryCreateModelReturnsOllamaEmbeddingModel()
    {
        var services = ServicesWithConfig();
        services.AddOllamaProvider();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEmbeddingProviderFactory>();

        var model = factory.CreateModel(new ModelConfiguration("embeddinggemma:latest"));

        await Assert.That(model).IsTypeOf<OllamaEmbeddingModel>();
    }
}
