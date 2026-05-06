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
using Microsoft.Extensions.Hosting;

namespace LlamaShears.Api;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Default is 30s, which means a hung Blazor WebSocket holds
        // shutdown for 30s before the host gives up and forcibly tears
        // it down. Drop to a few seconds so Ctrl+C feels like Ctrl+C.
        // The middleware in UseApi handles new reconnect attempts; this
        // is the upper bound on already-upgraded connections.
        builder.Services.Configure<HostOptions>(o =>
            o.ShutdownTimeout = TimeSpan.FromSeconds(3));

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
