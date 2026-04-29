using LlamaShears.Api;
using LlamaShears.Api.Host;
using LlamaShears.Data;
using LlamaShears.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddLlamaShearsUserConfiguration();
builder.Services.AddHostStartupTask<TemplateSeedingStartupTask>();
builder.AddApi();
builder.Services.AddLlamaShearsData();

var app = builder.Build();
app.UseApi();

await app.RunAsync();

public partial class Program;
