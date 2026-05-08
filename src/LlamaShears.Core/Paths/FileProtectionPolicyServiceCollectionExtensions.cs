using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Core.Paths;

public static class FileProtectionPolicyServiceCollectionExtensions
{
    public static IServiceCollection AddFileProtectionPolicy(
        this IServiceCollection services,
        Action<FileProtectionPolicyOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IFileProtectionPolicy, FileProtectionPolicy>();
        if (configure is not null)
        {
            services.Configure(configure);
        }
        return services;
    }
}
