using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Core.Eventing.Extensions;

public static class EventingServiceCollectionExtensions
{
    public static IServiceCollection AddEventHandler<TImplementation>(this IServiceCollection services)
        where TImplementation : class
    {
        services.TryAddActivatedSingleton<TImplementation>();
        return services;
    }
}
