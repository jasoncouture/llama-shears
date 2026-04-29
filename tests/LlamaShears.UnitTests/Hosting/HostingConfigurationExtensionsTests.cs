using LlamaShears.Hosting;
using LlamaShears.Hosting.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
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

    [Test]
    public async Task AddLlamaShearsUserConfiguration_inserts_after_existing_json_sources()
    {
        var builder = new ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json", optional: true);
        builder.AddJsonFile("appsettings.Development.json", optional: true);

        builder.AddLlamaShearsUserConfiguration();

        var ourIndex = IndexOfUserConfigSource(builder);
        var lastAppsettingsIndex = builder.Sources
            .Select((src, i) => (src, i))
            .Last(t => t.src is JsonConfigurationSource s && s.Path is "appsettings.json" or "appsettings.Development.json").i;

        await Assert.That(ourIndex).IsEqualTo(lastAppsettingsIndex + 1);
    }

    [Test]
    public async Task AddLlamaShearsUserConfiguration_lands_before_environment_variables()
    {
        var builder = new ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json", optional: true);
        builder.AddEnvironmentVariables();

        builder.AddLlamaShearsUserConfiguration();

        var ourIndex = IndexOfUserConfigSource(builder);
        var envIndex = builder.Sources
            .Select((src, i) => (src, i))
            .First(t => t.src is EnvironmentVariablesConfigurationSource).i;

        await Assert.That(ourIndex).IsLessThan(envIndex);
    }

    [Test]
    public async Task AddLlamaShearsUserConfiguration_with_no_existing_json_source_inserts_at_the_front()
    {
        var builder = new ConfigurationBuilder();
        builder.AddEnvironmentVariables();

        builder.AddLlamaShearsUserConfiguration();

        await Assert.That(builder.Sources[0]).IsTypeOf<JsonConfigurationSource>();
        await Assert.That(builder.Sources[1]).IsTypeOf<EnvironmentVariablesConfigurationSource>();
    }

    private static int IndexOfUserConfigSource(IConfigurationBuilder builder)
    {
        for (var i = 0; i < builder.Sources.Count; i++)
        {
            if (builder.Sources[i] is JsonConfigurationSource source &&
                source.FileProvider is PhysicalFileProvider provider &&
                Path.GetFullPath(Path.Combine(provider.Root, source.Path!)) == LlamaShearsPaths.ConfigFile)
            {
                return i;
            }
        }

        throw new InvalidOperationException("User config source not found.");
    }
}
