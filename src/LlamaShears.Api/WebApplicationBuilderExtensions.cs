using LlamaShears.Api.Authentication;
using LlamaShears.Api.Mcp;
using LlamaShears.Api.Web;
using LlamaShears.Core;
using LlamaShears.Core.Eventing;
using LlamaShears.Provider.Ollama;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Api;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOllamaProvider();
        builder.Services.AddEventingFramework();
        builder.Services.AddCore();
        builder.Services.AddAgentManager();
        builder.Services.AddAgentBearerAuthentication();
        builder.Services.AddMcp();
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddWebUi();

        return builder;
    }
}
