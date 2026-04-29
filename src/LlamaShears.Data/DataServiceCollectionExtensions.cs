using LlamaShears.Data.Hooks;
using LlamaShears.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LlamaShears.Data;

/// <summary>
/// Dependency-injection registration for the LlamaShears data layer:
/// options binding, save-change hooks, the single hook-running
/// interceptor, and the pooled <see cref="LlamaShearsDbContext"/>.
/// </summary>
public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Default configuration section bound to
    /// <see cref="LlamaShearsDataOptions"/>.
    /// </summary>
    public const string DefaultConfigurationSection = "Data";

    /// <summary>
    /// Registers the LlamaShears data services. Options are bound from
    /// <paramref name="configurationSection"/> on the
    /// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>
    /// resolved from DI.
    /// </summary>
    public static IServiceCollection AddLlamaShearsData(
        this IServiceCollection services,
        string configurationSection = DefaultConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

        services.AddOptions<LlamaShearsDataOptions>()
            .BindConfiguration(configurationSection);

        // Hook execution order is undefined and must not be relied
        // upon. If two hooks need a defined sequence, they belong in
        // a single hook. Each hook here operates on a disjoint slice
        // of entity state.
        services.AddDatabaseHook<IdGenerationHook>();
        services.AddDatabaseHook<CreatedHook>();
        services.AddDatabaseHook<LastModifiedHook>();

        services.AddSingleton<SaveChangesHookInterceptor>();

        services.AddDbContextPool<LlamaShearsDbContext>((sp, options) =>
        {
            var dataOptions = sp.GetRequiredService<IOptions<LlamaShearsDataOptions>>().Value;
            options.UseSqlite(dataOptions.ConnectionString);
            options.AddInterceptors(sp.GetRequiredService<SaveChangesHookInterceptor>());
        });

        return services;
    }

    public static IServiceCollection AddDatabaseHook<T>(this IServiceCollection services) where T : class, ISaveChangesHook
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISaveChangesHook, T>());
        return services;
    }
}
