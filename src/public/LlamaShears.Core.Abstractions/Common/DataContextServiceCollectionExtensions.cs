using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Core.Abstractions.Common;

/// <summary>
/// DI registration helpers for <see cref="IDataContextItemProvider"/>.
/// </summary>
public static class DataContextServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <typeparamref name="TProvider"/> as a singleton
        /// <see cref="IDataContextItemProvider"/> under
        /// <see cref="DataContextConstants.SingletonKey"/>. The data-context
        /// factory pulls these at its own construction time, so they may
        /// only depend on other singletons.
        /// </summary>
        public IServiceCollection AddSingletonDataProvider<TProvider>()
            where TProvider : class, IDataContextItemProvider
        {
            ArgumentNullException.ThrowIfNull(services);
            services.TryAddEnumerable(ServiceDescriptor.KeyedSingleton<IDataContextItemProvider, TProvider>(DataContextConstants.SingletonKey));
            return services;
        }

        /// <summary>
        /// Registers <typeparamref name="TProvider"/> as a scoped
        /// <see cref="IDataContextItemProvider"/>. Scoped providers are
        /// resolved from the call-site's scope on each context start, so
        /// they may depend on scoped, transient, or singleton services.
        /// </summary>
        public IServiceCollection AddScopedDataProvider<TProvider>()
            where TProvider : class, IDataContextItemProvider
        {
            ArgumentNullException.ThrowIfNull(services);
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDataContextItemProvider, TProvider>());
            return services;
        }
    }

    /// <summary>
    /// Returns the scoped <see cref="IDataContextItemProvider"/>
    /// registrations resolvable from <paramref name="serviceProvider"/>.
    /// Singleton providers are not returned here — the factory consumes
    /// those directly via its <c>[FromKeyedServices]</c> constructor.
    /// </summary>
    public static IEnumerable<IDataContextItemProvider> GetScopedDataProviders(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        return serviceProvider.GetServices<IDataContextItemProvider>();
    }
}
