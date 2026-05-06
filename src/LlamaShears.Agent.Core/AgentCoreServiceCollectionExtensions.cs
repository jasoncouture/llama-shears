using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LlamaShears.Agent.Core;

/// <summary>
/// Dependency-injection registration for the agent core: MessagePipe
/// (the internal eventing system), heartbeat options, and the
/// <see cref="AgentHeartbeatService"/> hosted service.
/// </summary>
public static class AgentCoreServiceCollectionExtensions
{
    /// <summary>
    /// Default configuration section bound to <see cref="AgentHeartbeatOptions"/>.
    /// </summary>
    public const string DefaultHeartbeatConfigurationSection = "Agents:Heartbeat";

    /// <summary>
    /// Registers MessagePipe and the agent heartbeat service. Heartbeat
    /// options are bound from <paramref name="heartbeatConfigurationSection"/>
    /// on the <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>
    /// resolved from DI.
    /// </summary>
    public static IServiceCollection AddAgentCore(
        this IServiceCollection services,
        string heartbeatConfigurationSection = DefaultHeartbeatConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(heartbeatConfigurationSection);

        services.AddMessagePipe();

        services.AddOptions<AgentHeartbeatOptions>()
            .BindConfiguration(heartbeatConfigurationSection);

        services.AddHostedService<AgentHeartbeatService>();

        return services;
    }
}
