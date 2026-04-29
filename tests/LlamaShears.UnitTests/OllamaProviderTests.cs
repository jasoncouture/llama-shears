using LlamaShears.Provider.Abstractions;
using LlamaShears.Provider.Ollama;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;

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
    public async Task AddOllamaProviderRegistersOllamaApiClient()
    {
        var services = ServicesWithConfig();

        services.AddOllamaProvider();

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IOllamaApiClient>();

        await Assert.That(client).IsNotNull();
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

        await Assert.That(factory.Name).IsEqualTo("OLLAMA");
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
}
