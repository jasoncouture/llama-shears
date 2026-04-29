using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LlamaShears.Agent.Core;

/// <summary>
/// Dependency-injection registration for the agent core: MessagePipe
/// (the internal eventing system), system-tick options, and the
/// <see cref="SystemTickService"/> hosted service.
/// </summary>
public static class AgentCoreServiceCollectionExtensions
{
    /// <summary>
    /// Default configuration section bound to <see cref="SystemTickOptions"/>.
    /// </summary>
    public const string DefaultSystemTickConfigurationSection = "Frame";

    /// <summary>
    /// Registers MessagePipe and the system-tick service. System-tick
    /// options are bound from <paramref name="systemTickConfigurationSection"/>
    /// on the <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>
    /// resolved from DI.
    /// </summary>
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
}
