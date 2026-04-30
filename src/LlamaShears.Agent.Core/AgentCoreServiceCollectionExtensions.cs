using LlamaShears.Agent.Abstractions;
using LlamaShears.Agent.Abstractions.Persistence;
using LlamaShears.Agent.Core.Persistence;
using LlamaShears.Agent.Core.SystemPrompt;
using LlamaShears.Hosting;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LlamaShears.Agent.Core;

public static class AgentCoreServiceCollectionExtensions
{
    public const string DefaultSystemTickConfigurationSection = "Frame";

    public const string DefaultAgentTokenStoreConfigurationSection = "AgentTokenStore";

    public static IServiceCollection AddAgentCore(
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
        services.TryAddSingleton<ISystemPromptDataProvider, SystemPromptDataProvider>();
        services.TryAddSingleton<IContextStore, JsonLineContextStore>();

        return services;
    }

    public static IServiceCollection AddAgentManager(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostStartupTaskRunner();
        services.AddShearsPaths();
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
}
