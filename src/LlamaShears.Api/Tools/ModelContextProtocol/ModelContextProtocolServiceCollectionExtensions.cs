using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Api.Tools.ModelContextProtocol.Memory;
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
        services.TryAddScoped<IAgentWorkspaceLocator, AgentWorkspaceLocator>();

        services.AddMcpServer()
            .WithHttpTransport()
            .WithTools<WhoamiTool>()
            .WithTools<RandomNumberTool>()
            .WithTools<ReadFileTool>()
            .WithTools<ListFilesTool>()
            .WithTools<WriteFileTool>()
            .WithTools<AppendFileTool>()
            .WithTools<DeleteFileTool>()
            .WithTools<RegexReplaceFileTool>()
            .WithTools<GrepTool>()
            .WithTools<StoreMemoryTool>()
            .WithTools<SearchMemoryTool>()
            .WithTools<IndexMemoryTool>();

        return services;
    }
}
