using LlamaShears.Core.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Debugging;

public static class EventBusFileLogServiceCollectionExtensions
{
    public const string DefaultConfigurationSection = "Debugging:Events:Logging";

    public static IServiceCollection AddEventBusFileLog(
        this IServiceCollection services,
        string configurationSection = DefaultConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

        services.AddOptions<EventBusFileLogOptions>().BindConfiguration(configurationSection);
        services.TryAddSingleton(TimeProvider.System);

        var inner = services.LastOrDefault(d => d.ServiceType == typeof(IEventBus))
            ?? throw new InvalidOperationException(
                $"{nameof(AddEventBusFileLog)} must be called after IEventBus has been registered.");
        services.Remove(inner);

        services.AddSingleton<IEventBus>(sp =>
        {
            var innerInstance = (IEventBus)CreateFromDescriptor(sp, inner);
            return ActivatorUtilities.CreateInstance<EventBusFileLogDecorator>(sp, innerInstance);
        });

        return services;
    }

    private static object CreateFromDescriptor(IServiceProvider sp, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is { } instance) return instance;
        if (descriptor.ImplementationFactory is { } factory) return factory.Invoke(sp);
        if (descriptor.ImplementationType is { } type) return ActivatorUtilities.CreateInstance(sp, type);
        throw new InvalidOperationException($"ServiceDescriptor for {descriptor.ServiceType} has no implementation.");
    }
}
