using LlamaShears.Data.Hooks;
using LlamaShears.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LlamaShears.Data;

public static class DataServiceCollectionExtensions
{
    public const string DefaultConfigurationSection = "Data";

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
