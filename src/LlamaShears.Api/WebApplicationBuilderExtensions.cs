using LlamaShears.Provider.Ollama;
using Microsoft.AspNetCore.Builder;

namespace LlamaShears.Api;

/// <summary>
/// Registers LlamaShears API services on a <see cref="WebApplicationBuilder"/>.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Registers all services required by the LlamaShears API.
    /// </summary>
    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOllamaProvider();

        return builder;
    }
}
