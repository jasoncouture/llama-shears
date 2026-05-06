using LlamaShears.Agent.Core;
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

        return builder;
    }
}
