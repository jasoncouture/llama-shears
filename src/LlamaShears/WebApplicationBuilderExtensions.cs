using LlamaShears.Api;
using LlamaShears.Debugging;
using LlamaShears.Hosting;
using LlamaShears.PluginLoaders;
using LlamaShears.Plugins.Host;
using Microsoft.Extensions.Configuration.Json;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LlamaShears;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddTelemetry(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IResourceDetector, AppResourceDetector>();
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddDetector(sp => new CompositeResourceDetector(sp.GetServices<IResourceDetector>())))
            .WithTracing(tracing => tracing
                .AddSource("LlamaShears*")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddMeter("LlamaShears*")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());
        builder.Logging.AddOpenTelemetry(logging => logging.AddOtlpExporter());

        return builder;
    }

    public static WebApplicationBuilder AddLocalConfiguration(this WebApplicationBuilder builder)
    {
        var args = Environment.GetCommandLineArgs();
        var skipLocalConfig =
            args.Any(a => string.Equals(a, "--no-local-config", StringComparison.OrdinalIgnoreCase))
            || string.Equals(
            Environment.GetEnvironmentVariable("LLAMASHEARS_NO_LOCAL_CONFIG"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!skipLocalConfig)
        {
            var dataRoot = builder.Configuration["Paths:DataRoot"];
            if (string.IsNullOrWhiteSpace(dataRoot))
            {
                dataRoot = "~/.llama-shears";
            }
            dataRoot = dataRoot.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            dataRoot = Directory.CreateDirectory(dataRoot).FullName;

            var overrideSource = new JsonConfigurationSource
            {
                Path = Path.Combine(dataRoot, "appsettings.json"),
                Optional = true,
                ReloadOnChange = true,
            };
            overrideSource.ResolveFileProvider();

            var sources = builder.Configuration.Sources;
            var insertAt = 0;
            for (var i = 0; i < sources.Count; i++)
            {
                if (sources[i] is JsonConfigurationSource)
                {
                    insertAt = i + 1;
                }
            }
            sources.Insert(insertAt, overrideSource);
        }

        return builder;
    }

    public static WebApplicationBuilder AddCore(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostStartupTask<TemplateSeedingStartupTask>();
        builder.AddApi();
        builder.Services.AddEventBusFileLog();
        return builder;
    }

    public static async Task<WebApplicationBuilder> AddPluginsAsync(this WebApplicationBuilder builder, CancellationToken cancellationToken)
    {
        var pluginPaths = Array.Empty<string>();

        await builder.Services.LoadPluginsAsync(failureCallback: null, cancellationToken, new PathPluginLoader(pluginPaths));
        return builder;
    }

}