using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Events;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Core.Eventing;

public static class EventingServiceCollectionExtensions
{
    public static IServiceCollection AddEventingFramework(this IServiceCollection services)
    {
        services.TryAddSingleton<EventBus>();
        services.ForwardService<IEventBus, EventBus>();
        services.ForwardService<IEventPublisher, EventBus>();
        services.TryAddSingleton<IPatternMatcher, PatternMatcher>();
        services.AddMessagePipe(o =>
        {
            o.EnableAutoRegistration = false;
            o.RequestHandlerLifetime = InstanceLifetime.Scoped;
            o.DefaultAsyncPublishStrategy = AsyncPublishStrategy.Parallel;
        });
        return services;
    }

    internal static IServiceCollection ForwardService<TService, TImplementation>(this IServiceCollection services)
        where TImplementation : notnull, TService
        where TService : class
    {
        services.AddTransient<TService>(static sp => sp.GetRequiredService<TImplementation>());
        return services;
    }
}
