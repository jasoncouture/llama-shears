using LlamaShears;
using LlamaShears.Api;
using LlamaShears.Core;
using LlamaShears.Hosting;
using LlamaShears.PluginLoaders;
using LlamaShears.Plugins.Host;
using Microsoft.Extensions.Configuration.Json;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddShearsPaths();
builder.Services.AddHostStartupTask<TemplateSeedingStartupTask>();
builder.AddApi();
builder.Services.AddResponseCompression();
builder.Services.AddResponseCaching();

var pluginPaths = Array.Empty<string>();

await builder.Services.LoadPluginsAsync(failureCallback: null, CancellationToken.None, new PathPluginLoader(pluginPaths));

builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
var app = builder.Build();

app.UseResponseCompression();
app.UseResponseCaching();
await app.UsePluginsAsync(app.Lifetime.ApplicationStopping);

app.UseApi();

await app.RunAsync();

public partial class Program;
