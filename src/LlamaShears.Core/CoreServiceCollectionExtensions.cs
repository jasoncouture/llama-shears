using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Paths;
using LlamaShears.Core.Persistence;
using LlamaShears.Core.SystemPrompt;
using LlamaShears.Hosting;
using MessagePipe;
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

    public static IServiceCollection AddCore(
        this IServiceCollection services,
        string systemTickConfigurationSection = DefaultSystemTickConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(systemTickConfigurationSection);

        services.AddMessagePipe();
        services.AddShearsPaths();

        services.AddOptions<SystemTickOptions>()
            .BindConfiguration(systemTickConfigurationSection);

        services.AddHostedService<SystemTickService>();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ISystemPromptProvider, HardcodedSystemPromptProvider>();
        services.TryAddSingleton<IContextStore, JsonLineContextStore>();
        services.TryAddSingleton<IAgentConfigProvider, AgentConfigProvider>();

        return services;
    }

    public static IServiceCollection AddAgentManager(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostStartupTaskRunner();
        services.AddShearsPaths();
        services.TryAddSingleton<IAgentConfigProvider, AgentConfigProvider>();
        services.TryAddSingleton<AgentManager>();
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
