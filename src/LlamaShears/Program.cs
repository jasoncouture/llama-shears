using LlamaShears;
using LlamaShears.Api;
using LlamaShears.Core;
using LlamaShears.Hosting;
using LlamaShears.PluginLoaders;
using LlamaShears.Plugins.Host;

var builder = WebApplication.CreateBuilder(args);
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
