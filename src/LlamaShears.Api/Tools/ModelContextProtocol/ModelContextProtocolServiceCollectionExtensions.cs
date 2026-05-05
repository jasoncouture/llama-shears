using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Api.Tools.ModelContextProtocol;

public static class ModelContextProtocolServiceCollectionExtensions
{
    public static IServiceCollection AddModelContextProtocol(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.TryAddSingleton<IInternalModelContextProtocolServer, InternalModelContextProtocolServer>();

        services.AddMcpServer()
            .WithHttpTransport()
            .WithTools<WhoamiTool>()
            .WithTools<RandomNumberTool>();

        return services;
    }
}
