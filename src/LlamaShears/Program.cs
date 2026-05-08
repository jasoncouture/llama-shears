using LlamaShears;
using LlamaShears.Api;
using LlamaShears.Core;
using LlamaShears.Hosting;
using LlamaShears.PluginLoaders;
using LlamaShears.Plugins.Host;
using Microsoft.Extensions.Configuration.Json;

var builder = WebApplication.CreateBuilder(args);

// Resolve the data-root path: explicit Paths:DataRoot wins; otherwise
// default to ~/.llama-shears. ~ expands via UserProfile so the same
// config string works on Windows. CreateDirectory both materialises
// the dir and gives back the absolute path.
var dataRoot = builder.Configuration["Paths:DataRoot"];
if (string.IsNullOrWhiteSpace(dataRoot))
{
    dataRoot = "~/.llama-shears";
}
dataRoot = dataRoot.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
dataRoot = Directory.CreateDirectory(dataRoot).FullName;

// Layer <dataRoot>/appsettings.json into the config chain. Insert
// position must be AFTER the last bundled JSON source (so the file
// overrides appsettings.json / appsettings.{Environment}.json /
// user-secrets) and BEFORE any non-JSON source (so env vars and
// command-line args still override the file). AddJsonFile appends —
// that lands the source past the env/cli sources and breaks the
// precedence — so we construct the source by hand and Insert.
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

builder.Services.AddShearsPaths();
builder.Services.AddHostStartupTask<TemplateSeedingStartupTask>();
builder.AddApi();

var pluginPaths = Array.Empty<string>();

await builder.Services.LoadPluginsAsync(failureCallback: null, CancellationToken.None, new PathPluginLoader(pluginPaths));

var app = builder.Build();

await app.UsePluginsAsync(app.Lifetime.ApplicationStopping);

app.UseApi();

await app.RunAsync();

public partial class Program;
