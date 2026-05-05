using LlamaShears.Api.Authentication;
using LlamaShears.Api.Tools.ModelContextProtocol;
using LlamaShears.Api.Web;
using LlamaShears.Core;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Eventing.Extensions;
using LlamaShears.Core.Persistence;
using LlamaShears.Provider.Ollama;
using LlamaShears.Provider.Onnx.Embeddings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Api;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOllamaProvider();
        builder.Services.AddOnnxEmbeddingsProvider();
        builder.Services.AddEventingFramework();
        builder.Services.AddCore();
        builder.Services.AddAgentManager();
        builder.Services.AddEventHandler<AgentTurnContextPersister>();
        builder.Services.AddEventHandler<AgentTurnLogger>();
        builder.Services.AddAgentBearerAuthentication();
        builder.Services.AddModelContextProtocol();
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddWebUi();

        return builder;
    }
}
