using LlamaShears.Provider.Abstractions;
using LlamaShears.Provider.Ollama;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace LlamaShears.UnitTests;

public class OllamaProviderTests
{
    [Test]
    public async Task AddOllamaProvider_RegistersProviderFactory()
    {
        var services = new ServiceCollection();

        services.AddOllamaProvider();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IProviderFactory>();

        await Assert.That(factory).IsTypeOf<OllamaProviderFactory>();
    }

    [Test]
    public async Task AddOllamaProvider_RegistersOllamaApiClient()
    {
        var services = new ServiceCollection();

        services.AddOllamaProvider();

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IOllamaApiClient>();

        await Assert.That(client).IsNotNull();
    }

    [Test]
    public async Task AddOllamaProvider_AppliesConfigureDelegate()
    {
        var services = new ServiceCollection();
        var customUri = new Uri("http://ollama.example:9999");

        services.AddOllamaProvider(o => o.BaseUri = customUri);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OllamaProviderOptions>>().Value;

        await Assert.That(options.BaseUri).IsEqualTo(customUri);
    }

    [Test]
    public async Task OllamaProviderOptions_DefaultBaseUri_IsLocalhost11434()
    {
        var options = new OllamaProviderOptions();

        await Assert.That(options.BaseUri).IsEqualTo(new Uri("http://localhost:11434"));
    }

    [Test]
    public async Task Factory_Name_IsOllama()
    {
        var services = new ServiceCollection();
        services.AddOllamaProvider();
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IProviderFactory>();

        await Assert.That(factory.Name).IsEqualTo("OLLAMA");
    }

    [Test]
    public async Task Factory_CreateModel_ReturnsOllamaLanguageModel()
    {
        var services = new ServiceCollection();
        services.AddOllamaProvider();
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IProviderFactory>();

        var model = factory.CreateModel(new ModelConfiguration("llama3"));

        await Assert.That(model).IsTypeOf<OllamaLanguageModel>();
    }
}
