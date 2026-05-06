using LlamaShears.Hosting.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace LlamaShears.Hosting;

public static class HostingConfigurationExtensions
{
    /// <summary>
    /// Adds the host's user-config JSON file as an optional, watched
    /// source. Inserted after the last <see cref="JsonConfigurationSource"/>
    /// already in the builder so env-vars and CLI args registered later
    /// still take precedence.
    /// </summary>
    public static IConfigurationBuilder AddLlamaShearsUserConfiguration(this IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var source = new JsonConfigurationSource
        {
            Path = LlamaShearsPaths.ConfigFile,
            Optional = true,
            ReloadOnChange = true,
        };
        source.ResolveFileProvider();

        var lastJsonIndex = -1;
        for (var i = 0; i < builder.Sources.Count; i++)
        {
            if (builder.Sources[i] is JsonConfigurationSource)
            {
                lastJsonIndex = i;
            }
        }

        builder.Sources.Insert(lastJsonIndex + 1, source);

        return builder;
    }
}
