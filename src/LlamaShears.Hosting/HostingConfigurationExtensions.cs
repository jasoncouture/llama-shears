using LlamaShears.Hosting.Abstractions;
using Microsoft.Extensions.Configuration;

namespace LlamaShears.Hosting;

/// <summary>
/// Configuration-builder extensions that wire the host's user-data
/// JSON file into <see cref="IConfiguration"/>.
/// </summary>
public static class HostingConfigurationExtensions
{
    /// <summary>
    /// Adds <see cref="LlamaShearsPaths.ConfigFile"/> to the
    /// configuration builder as an optional, watched JSON source. The
    /// file is watched for content changes; bindings backed by
    /// <see cref="IOptionsMonitor{T}"/> see updates without a restart.
    /// Per ADR-0011 the file lives at
    /// <c>&lt;UserProfile&gt;/.llama-shears/config.json</c>.
    /// </summary>
    public static IConfigurationBuilder AddLlamaShearsUserConfiguration(this IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddJsonFile(LlamaShearsPaths.ConfigFile, optional: true, reloadOnChange: true);

        return builder;
    }
}
