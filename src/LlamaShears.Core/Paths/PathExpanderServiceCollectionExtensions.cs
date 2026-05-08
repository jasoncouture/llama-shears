using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Core.Paths;

public static class PathExpanderServiceCollectionExtensions
{
    public static IServiceCollection AddPathExpander(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IPathExpander, PathExpander>();
        return services;
    }
}
