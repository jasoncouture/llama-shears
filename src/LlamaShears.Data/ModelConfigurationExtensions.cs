using LlamaShears.Data.Abstractions;
using LlamaShears.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LlamaShears.Data;

public static class ModelConfigurationExtensions
{
    public static ModelBuilder ConfigureAllModels(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ConfigureModel<Session>();
        modelBuilder.ConfigureModel<SessionMessage>();
        return modelBuilder;
    }

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
