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

    public static IServiceCollection AddAgentCore(
        this IServiceCollection services,
        string systemTickConfigurationSection = DefaultSystemTickConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(systemTickConfigurationSection);

        services.AddMessagePipe();

        services.AddOptions<SystemTickOptions>()
            .BindConfiguration(systemTickConfigurationSection);

        services.AddHostedService<SystemTickService>();

        return services;
    }

    public static IServiceCollection AddAgentManager(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostStartupTaskRunner();
        services.TryAddSingleton<AgentManager>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostStartupTask, AgentManager>(
                sp => sp.GetRequiredService<AgentManager>()));

        return services;
    }
}
