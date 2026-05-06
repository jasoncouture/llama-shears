using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LlamaShears.Agent.Core;

/// <summary>
/// Dependency-injection registration for the agent core: MessagePipe
/// (the internal eventing system), frame-tick options, and the
/// <see cref="FrameTickService"/> hosted service.
/// </summary>
public static class AgentCoreServiceCollectionExtensions
{
    /// <summary>
    /// Default configuration section bound to <see cref="FrameTickOptions"/>.
    /// </summary>
    public const string DefaultFrameTickConfigurationSection = "Frame";

    /// <summary>
    /// Registers MessagePipe and the frame-tick service. Frame-tick
    /// options are bound from <paramref name="frameTickConfigurationSection"/>
    /// on the <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>
    /// resolved from DI.
    /// </summary>
    public static IServiceCollection AddAgentCore(
        this IServiceCollection services,
        string frameTickConfigurationSection = DefaultFrameTickConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(frameTickConfigurationSection);

        services.AddMessagePipe();

        services.AddOptions<FrameTickOptions>()
            .BindConfiguration(frameTickConfigurationSection);

        services.AddHostedService<FrameTickService>();

        return services;
    }
}
