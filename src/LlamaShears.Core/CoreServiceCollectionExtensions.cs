using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Agent.Todo;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Caching;
using LlamaShears.Core.Common;
using LlamaShears.Core.Context;
using LlamaShears.Core.Cron;
using LlamaShears.Core.DataContext;
using LlamaShears.Core.Memory;
using LlamaShears.Core.Paths;
using LlamaShears.Core.Persistence;
using LlamaShears.Core.PromptContext;
using LlamaShears.Core.Provider;
using LlamaShears.Core.Seeding;
using LlamaShears.Core.Sessions;
using LlamaShears.Core.SystemPrompt;
using LlamaShears.Core.Templating;
using LlamaShears.Core.Todo;
using LlamaShears.Core.Tools.ModelContextProtocol;
using LlamaShears.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Core;

public static class CoreServiceCollectionExtensions
{
    public const string DefaultSystemTickConfigurationSection = "Frame";
    public const string DefaultAgentTokenStoreConfigurationSection = "AgentTokenStore";
    public const string DefaultShearsPathsConfigurationSection = "Paths";
    public const string DefaultFileParserCacheConfigurationSection = "FileParserCache";
    public const string DefaultModelContextProtocolConfigurationSection = "ModelContextProtocol";
    public const string DefaultMemoryConfigurationSection = "Memory";

    public static IServiceCollection AddCore(
        this IServiceCollection services,
        string systemTickConfigurationSection = DefaultSystemTickConfigurationSection,
        string fileParserCacheConfigurationSection = DefaultFileParserCacheConfigurationSection,
        string modelContextProtocolConfigurationSection = DefaultModelContextProtocolConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(systemTickConfigurationSection);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileParserCacheConfigurationSection);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelContextProtocolConfigurationSection);

        services.AddMemoryCache();
        services.AddShearsPaths();
        services.AddCommonServices();
        services.AddCaching(fileParserCacheConfigurationSection);
        services.AddSystemTick(systemTickConfigurationSection);
        services.AddTemplating();
        services.AddPromptProviders();
        services.AddContextStore();
        services.AddDataContext();
        services.AddInference();
        services.AddTodo();
        services.AddCompaction();
        services.AddMemoryStore();
        services.AddCron();
        services.AddSessions();
        services.AddAgentRuntime();
        services.AddModelContextProtocolPlumbing(modelContextProtocolConfigurationSection);

        return services;
    }

    public static IServiceCollection AddAgentManager(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostStartupTaskRunner();
        services.AddShearsPaths();
        services.AddCommonServices();
        services.TryAddSingleton<IAgentConfigProvider, AgentConfigProvider>();
        services.TryAddSingleton<AgentManager>();
        services.TryAddSingleton<IAgentManager>(sp => sp.GetRequiredService<AgentManager>());
        services.AddHostedService(sp => sp.GetRequiredService<AgentManager>());
        services.AddHostedService<AgentConfigSupervisor>();

        return services;
    }

    public static IServiceCollection AddAgentTokenStore(
        this IServiceCollection services,
        string configurationSection = DefaultAgentTokenStoreConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

        services.AddOptions<AgentTokenStoreOptions>()
            .BindConfiguration(configurationSection);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<InMemoryAgentTokenStore>();
        services.TryAddSingleton<IAgentTokenStore>(
            sp => sp.GetRequiredService<InMemoryAgentTokenStore>());
        services.AddHostedService<AgentTokenStoreSweeper>();

        return services;
    }

    public static IServiceCollection AddShearsPaths(
        this IServiceCollection services,
        string configurationSection = DefaultShearsPathsConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

        services.AddOptions<ShearsPathsOptions>()
            .BindConfiguration(configurationSection);
        services.TryAddSingleton<IShearsPaths, ShearsPaths>();

        return services;
    }

    public static IServiceCollection AddAgentService<T>(this IServiceCollection services)
        where T : class, IAgentService
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAgentService, T>());
        return services;
    }

    private static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IUriMerger, UriMerger>();
        services.TryAddSingleton<IDirectorySeeder, DirectorySeeder>();
        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services, string section)
    {
        services.TryAddSingleton(typeof(IShearsCache<>), typeof(ShearsCache<>));
        services.TryAddSingleton(typeof(IFileParserCache<>), typeof(FileParserCache<>));
        services.AddOptions<FileParserCacheOptions>()
            .BindConfiguration(section)
            .Validate(o => o.TimeToLive > TimeSpan.Zero, "FileParserCache:TimeToLive must be strictly positive.")
            .ValidateOnStart();
        return services;
    }

    private static IServiceCollection AddSystemTick(this IServiceCollection services, string section)
    {
        services.AddOptions<SystemTickOptions>()
            .BindConfiguration(section);
        services.AddHostedService<SystemTickService>();
        return services;
    }

    private static IServiceCollection AddTemplating(this IServiceCollection services)
    {
        services.TryAddSingleton<ITemplateRenderer, TemplateRenderer>();
        services.TryAddSingleton<ITemplateFileLocator, TemplateFileLocator>();
        return services;
    }

    private static IServiceCollection AddPromptProviders(this IServiceCollection services)
    {
        services.AddOptions<FilesystemSystemPromptOptions>();
        services.TryAddSingleton<ISystemPromptProvider, FilesystemSystemPromptProvider>();
        services.AddOptions<FilesystemPromptContextOptions>();
        services.TryAddSingleton<IPromptContextProvider, FilesystemPromptContextProvider>();
        return services;
    }

    private static IServiceCollection AddContextStore(this IServiceCollection services)
    {
        services.TryAddSingleton<IContextStore, JsonLineContextStore>();
        services.TryAddSingleton<IAgentConfigProvider, AgentConfigProvider>();
        services.TryAddSingleton<IAgentContextProvider, AgentContextProvider>();
        return services;
    }

    private static IServiceCollection AddDataContext(this IServiceCollection services)
    {
        services.TryAddSingleton<IDataContextFactory, DataContextFactory>();
        services.TryAddScoped<IDataContextScope>(sp =>
            sp.GetRequiredService<IDataContextFactory>().Current
                ?? throw new InvalidOperationException("No ambient data scope is available"));
        services.AddSingletonDataProvider<HostDataProvider>();
        services.AddScopedDataProvider<TodoListDataProvider>();
        services.AddScopedDataProvider<WallClockDataProvider>();
        services.AddScopedDataProvider<WorkspaceContextDataProvider>();
        return services;
    }

    private static IServiceCollection AddInference(this IServiceCollection services)
    {
        services.TryAddScoped<IAgentStateTracker, AgentStateTracker>();
        services.TryAddScoped<IInferenceRunner, InferenceRunner>();
        services.TryAddScoped<ILanguageModel>(sp =>
        {
            var dataScope = sp.GetRequiredService<IDataContextScope>();
            var modelConfig = dataScope.GetModelConfiguration();
            var providerFactory = sp.GetServices<IProviderFactory>()
                .FirstOrDefault(p => string.Equals(p.Name, modelConfig.Id.Provider, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"No provider factory registered with name '{modelConfig.Id.Provider}'.");
            return providerFactory.CreateModel(modelConfig);
        });
        services.TryAddSingleton<IModelTextFormatter, ModelTextFormatter>();
        return services;
    }

    private static IServiceCollection AddTodo(this IServiceCollection services)
    {
        services.TryAddScoped<ITodoStorage, TodoStorage>();
        return services;
    }

    private static IServiceCollection AddCompaction(this IServiceCollection services)
    {
        services.TryAddScoped<IContextCompactor, ContextCompactor>();
        services.AddAgentService<CompactionAgentService>();
        return services;
    }

    private static IServiceCollection AddMemoryStore(this IServiceCollection services)
    {
        services.AddOptions<MemoryServiceOptions>()
            .BindConfiguration(DefaultMemoryConfigurationSection);
        services.TryAddSingleton<SqliteMemoryService>();
        services.TryAddTransient<IMemoryStore>(sp => sp.GetRequiredService<SqliteMemoryService>());
        services.TryAddTransient<IMemorySearcher>(sp => sp.GetRequiredService<SqliteMemoryService>());
        services.TryAddTransient<IMemoryIndexer>(sp => sp.GetRequiredService<SqliteMemoryService>());
        services.AddHostedService<MemoryIndexerBackgroundService>();
        return services;
    }

    private static IServiceCollection AddSessions(this IServiceCollection services)
    {
        services.TryAddSingleton<ISessionFactory, SessionFactory>();
        return services;
    }

    private static IServiceCollection AddAgentRuntime(this IServiceCollection services)
    {
        services.TryAddSingleton<IAgentLockManager, AgentLockManager>();
        services.TryAddSingleton<IAgentFactory, AgentFactory>();
        services.TryAddScoped<IAgentLock, AgentLock>();
        services.TryAddScoped<IAgentIterationRunner, AgentIterationRunner>();
        services.TryAddScoped<IAgent, Agent>();
        return services;
    }

    private static IServiceCollection AddModelContextProtocolPlumbing(this IServiceCollection services, string section)
    {
        services.AddOptions<ModelContextProtocolOptions>()
            .BindConfiguration(section);
        services.TryAddSingleton<IModelContextProtocolToolDiscovery, ModelContextProtocolToolDiscovery>();
        services.TryAddSingleton<IModelContextProtocolServerRegistry, ModelContextProtocolServerRegistry>();
        services.TryAddSingleton<IToolCallDispatcher, ModelContextProtocolToolCallDispatcher>();
        services.TryAddTransient<LoopbackBearerHandler>();
        services.TryAddTransient<ModelContextProtocolRoutingHandler>();
        services.AddHttpClient<IModelContextProtocolClient, ModelContextProtocolClient>()
            .AddHttpMessageHandler<ModelContextProtocolRoutingHandler>()
            .AddHttpMessageHandler<LoopbackBearerHandler>();
        return services;
    }
}
