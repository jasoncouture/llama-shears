using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Core.Abstractions.Agent.Pipeline;

/// <summary>
/// DI helpers for the per-batch agent middleware onion.
/// </summary>
public static class AgentPipelineServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IAgentPipeline"/> fold. Idempotent.
    /// Called automatically by <see cref="AddAgentMiddleware{T}"/>.
    /// </summary>
    /// <param name="services">The collection to add to.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddAgentPipeline(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<IAgentPipeline, AgentPipeline>();
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="T"/> as a scoped
    /// <see cref="IAgentMiddleware"/> and ensures
    /// <see cref="IAgentPipeline"/> is registered.
    /// <typeparamref name="T"/>'s <see cref="IAgentMiddleware.Order"/>
    /// places the step; built-ins use <see cref="AgentMiddlewareOrder"/>
    /// (spaced 1000). Registration order is not the onion order.
    /// </summary>
    /// <typeparam name="T">The middleware type to add.</typeparam>
    /// <param name="services">The collection to add to.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddAgentMiddleware<T>(this IServiceCollection services)
        where T : class, IAgentMiddleware
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddAgentPipeline();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAgentMiddleware, T>());
        return services;
    }
}
