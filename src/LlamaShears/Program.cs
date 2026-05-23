using LlamaShears;
using LlamaShears.Api;
using LlamaShears.Core;
using LlamaShears.Debugging;
using LlamaShears.Hosting;
using LlamaShears.PluginLoaders;
using LlamaShears.Plugins.Host;
using Microsoft.Extensions.Configuration.Json;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.AddLocalConfiguration();
builder.AddTelemetry();
builder.AddCore();


builder.Services.AddResponseCompression();
builder.Services.AddResponseCaching();


await builder.AddPluginsAsync(CancellationToken.None);

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
