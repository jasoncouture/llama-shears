using LlamaShears.Agent.Core;
using LlamaShears.Api.Authentication;
using LlamaShears.Api.Mcp;
using LlamaShears.Provider.Ollama;
using Microsoft.AspNetCore.Builder;

namespace LlamaShears.Api;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOllamaProvider();
        builder.Services.AddAgentCore();
        builder.Services.AddAgentManager();
        builder.Services.AddAgentBearerAuthentication();
        builder.Services.AddMcp();

        return builder;
    }
}
