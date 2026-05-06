using LlamaShears.Api;
using LlamaShears.Data;
using LlamaShears.Provider.Abstractions;

var builder = WebApplication.CreateBuilder(args);
builder.AddApi();
builder.Services.AddLlamaShearsData();

var app = builder.Build();
app.UseApi();

await using var scope = app.Services.CreateAsyncScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var factory = scope.ServiceProvider.GetRequiredService<IProviderFactory>();

logger.LogInformation("Listing models from provider {ProviderName}", factory.Name);

await foreach (var model in factory.ListModelsAsync())
{
    logger.LogInformation(
        "Model: {ModelId} (display: {DisplayName}, description: {Description})",
        model.ModelId,
        model.DisplayName,
        model.Description);
}

return 0;

public partial class Program;
