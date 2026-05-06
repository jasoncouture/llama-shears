using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.Seeding;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Abstractions.Templating;
using LlamaShears.Core.Caching;
using LlamaShears.Core.Context;
using LlamaShears.Core.Paths;
using LlamaShears.Core.Persistence;
using LlamaShears.Core.Seeding;
using LlamaShears.Core.SystemPrompt;
using LlamaShears.Core.Templating;
using LlamaShears.Core.Tools.ModelContextProtocol;
using LlamaShears.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core;

public static class CoreServiceCollectionExtensions
{
    public const string DefaultSystemTickConfigurationSection = "Frame";

    public const string DefaultAgentTokenStoreConfigurationSection = "AgentTokenStore";

    public const string DefaultShearsPathsConfigurationSection = "Paths";

    public const string DefaultFileParserCacheConfigurationSection = "FileParserCache";

    public const string DefaultModelContextProtocolConfigurationSection = "ModelContextProtocol";

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
        services.TryAddSingleton(typeof(IShearsCache<>), typeof(ShearsCache<>));
        services.TryAddSingleton(typeof(IFileParserCache<>), typeof(FileParserCache<>));

        services.AddOptions<FileParserCacheOptions>()
            .BindConfiguration(fileParserCacheConfigurationSection)
            .Validate(o => o.TimeToLive > TimeSpan.Zero, "FileParserCache:TimeToLive must be strictly positive.")
            .ValidateOnStart();

        services.AddOptions<SystemTickOptions>()
            .BindConfiguration(systemTickConfigurationSection);

        services.AddHostedService<SystemTickService>();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IDirectorySeeder, DirectorySeeder>();
        services.TryAddSingleton<ITemplateRenderer, TemplateRenderer>();
        services.AddOptions<FilesystemSystemPromptOptions>();
        services.TryAddSingleton<ISystemPromptProvider, FilesystemSystemPromptProvider>();
        services.TryAddSingleton<IContextStore, JsonLineContextStore>();
        services.TryAddSingleton<IAgentConfigProvider, AgentConfigProvider>();
        services.TryAddSingleton<IAgentContextProvider, AgentContextProvider>();
        services.TryAddSingleton<IInferenceRunner, InferenceRunner>();
        services.TryAddSingleton<IContextCompactor, ContextCompactor>();
        services.AddHostedService<EagerCompactor>();

        services.AddOptions<ModelContextProtocolOptions>()
            .BindConfiguration(modelContextProtocolConfigurationSection);

        services.TryAddSingleton<ICurrentAgentAccessor, CurrentAgentAccessor>();
        services.TryAddTransient<LoopbackBearerHandler>();
        services.AddHttpClient(ModelContextProtocolToolDiscovery.HttpClientName)
            .AddHttpMessageHandler<LoopbackBearerHandler>();
        services.TryAddSingleton<IModelContextProtocolToolDiscovery, ModelContextProtocolToolDiscovery>();
        services.TryAddSingleton<IModelContextProtocolServerRegistry, ModelContextProtocolServerRegistry>();

        return services;
    }

    public static IServiceCollection AddAgentManager(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostStartupTaskRunner();
        services.AddShearsPaths();
        services.TryAddSingleton<IDirectorySeeder, DirectorySeeder>();
        services.TryAddSingleton<IAgentConfigProvider, AgentConfigProvider>();
        services.TryAddSingleton<AgentManager>();
        services.TryAddSingleton<IAgentManager>(sp => sp.GetRequiredService<AgentManager>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostStartupTask, AgentManager>(
                sp => sp.GetRequiredService<AgentManager>()));

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
}
