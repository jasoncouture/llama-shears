using LlamaShears.Api.Tools.ModelContextProtocol.Cron;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Api.Tools.ModelContextProtocol.Memory;
using LlamaShears.Api.Tools.ModelContextProtocol.Shell;
using LlamaShears.Api.Tools.ModelContextProtocol.Todo;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Paths;
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

        services
            .AddPathExpander()
            .AddFileProtectionPolicy(options =>
            {
                options.Rules.Add(new ProtectedFile(".git", ProtectionMode.Delete, FileType.Directory, "git metadata"));
                options.Rules.Add(new ProtectedFile(".git/**", ProtectionMode.Delete, FileType.Any, "git metadata"));
                options.Rules.Add(new ProtectedFile("**/.git", ProtectionMode.Delete, FileType.Directory, "nested git metadata"));
                options.Rules.Add(new ProtectedFile("**/.git/**", ProtectionMode.Delete, FileType.Any, "nested git metadata"));
                options.Rules.Add(new ProtectedFile("*.md", ProtectionMode.Delete, FileType.File, "agent root markdown"));
                options.Rules.Add(new ProtectedFile(".gitignore", ProtectionMode.Delete | ProtectionMode.Write, FileType.File, "workspace .gitignore"));
                options.Rules.Add(new ProtectedFile("TODO.md", ProtectionMode.Read | ProtectionMode.Write | ProtectionMode.Delete, FileType.File, "host-owned TODO list"));
            });

        services.AddMcpServer()
            .WithHttpTransport()
            .WithTools<ReadFileTool>()
            .WithTools<ListFilesTool>()
            .WithTools<WriteFileTool>()
            .WithTools<AppendFileTool>()
            .WithTools<DeleteFileTool>()
            .WithTools<MoveFileTool>()
            .WithTools<RegexReplaceFileTool>()
            .WithTools<GrepTool>()
            .WithTools<StoreMemoryTool>()
            .WithTools<SearchMemoryTool>()
            .WithTools<IndexMemoryTool>()
            .WithTools<ScheduleCronTool>()
            .WithTools<ListCronTool>()
            .WithTools<CancelCronTool>()
            .WithTools<EditCronTool>()
            .WithTools<TriggerCronTool>()
            .WithTools<TodoTools>()
            .WithTools<ShellTools>();

        return services;
    }
}
