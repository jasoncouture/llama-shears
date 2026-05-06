using LlamaShears.Hosting.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

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
    /// <para>
    /// The source is inserted directly after the last existing
    /// <see cref="JsonConfigurationSource"/> in the builder rather
    /// than appended. That keeps the user file as a JSON layer — it
    /// overrides <c>appsettings*.json</c> and user secrets (which are
    /// also JSON) — while environment variables and command-line
    /// arguments registered later in the pipeline still take
    /// precedence. If no JSON source is present, the user file is
    /// inserted at the front so the standard environment/CLI layers
    /// continue to win.
    /// </para>
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
