using LlamaShears.Data.Abstractions;
using LlamaShears.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LlamaShears.Data;

/// <summary>
/// Extension methods that wire up the LlamaShears entity model. Each
/// entity owns its own per-entity mapping via
/// <see cref="IModelConfigurable{TSelf}"/>; this dispatcher applies
/// the conventions implied by <see cref="IDataObject"/>,
/// <see cref="ICreated"/>, and <see cref="ILastModified"/> first,
/// then hands the narrowed builder to the entity for its own
/// configuration.
/// </summary>
public static class ModelConfigurationExtensions
{
    /// <summary>
    /// Configures every model owned by <see cref="LlamaShearsDbContext"/>.
    /// </summary>
    public static ModelBuilder ConfigureAllModels(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ConfigureModel<Agent>();
        modelBuilder.ConfigureModel<Session>();
        modelBuilder.ConfigureModel<SessionMessage>();
        modelBuilder.ConfigureModel<AgentSession>();
        return modelBuilder;
    }

    /// <summary>
    /// Applies interface-driven conventions and then the entity's own
    /// <see cref="IModelConfigurable{TSelf}.ConfigureModel"/>.
    /// </summary>
    public static ModelBuilder ConfigureModel<T>(this ModelBuilder modelBuilder)
        where T : class, IModelConfigurable<T>
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var entityBuilder = modelBuilder.Entity<T>();

        if (typeof(T).IsAssignableTo(typeof(IDataObject)))
        {
            entityBuilder.HasKey(nameof(IDataObject.Id));
            entityBuilder.Property(nameof(IDataObject.Id)).ValueGeneratedNever();
        }

        if (typeof(T).IsAssignableTo(typeof(ICreated)))
        {
            entityBuilder.Property(nameof(ICreated.Created)).IsRequired();
        }

        if (typeof(T).IsAssignableTo(typeof(ILastModified)))
        {
            entityBuilder.Property(nameof(ILastModified.LastModified)).IsRequired();
        }

        T.ConfigureModel(entityBuilder);
        return modelBuilder;
    }
}
