using LlamaShears.Core.Abstractions.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Api.Web.Services.SlashCommands;

public static class SlashCommandsServiceCollectionExtensions
{
    public static IServiceCollection AddSlashCommands(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<ISlashCommandRegistry, SlashCommandRegistry>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISlashCommand, ClearCommand>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISlashCommand, ArchiveCommand>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISlashCommand, CompactCommand>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISlashCommand, RestartCommand>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISlashCommand, InterruptCommand>());

        return services;
    }
}
