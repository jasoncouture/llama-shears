using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Api.Mcp;

public static class McpServiceCollectionExtensions
{
    public static IServiceCollection AddMcp(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();

        services.AddMcpServer()
            .WithHttpTransport()
            .WithTools<WhoamiTool>();

        return services;
    }
}
