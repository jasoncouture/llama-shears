using LlamaShears.Hosting;
using LlamaShears.Hosting.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

namespace LlamaShears.UnitTests.Hosting;

public sealed class HostingConfigurationExtensionsTests
{
    [Test]
    public async Task AddLlamaShearsUserConfiguration_registers_optional_watched_json_source_for_ConfigFile()
    {
        var builder = new ConfigurationBuilder();

        builder.AddLlamaShearsUserConfiguration();

        var jsonSource = builder.Sources.OfType<JsonConfigurationSource>().Single();

        await Assert.That(jsonSource.Optional).IsTrue();
        await Assert.That(jsonSource.ReloadOnChange).IsTrue();

        var fileProvider = jsonSource.FileProvider as PhysicalFileProvider;
        await Assert.That(fileProvider).IsNotNull();

        var resolved = Path.GetFullPath(Path.Combine(fileProvider!.Root, jsonSource.Path!));
        await Assert.That(resolved).IsEqualTo(LlamaShearsPaths.ConfigFile);
    }
}
