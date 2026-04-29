using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LlamaShears.Data.Abstractions;

/// <summary>
/// Implemented by entities that own their EF Core mapping. The mapping
/// is invoked from <c>LlamaShearsDbContext.OnModelCreating</c> via a
/// thin generic dispatcher; entities receive only the
/// <see cref="EntityTypeBuilder{TSelf}"/> for themselves so they cannot
/// reach into other entity mappings.
/// </summary>
/// <typeparam name="TSelf">The implementing entity type.</typeparam>
public interface IModelConfigurable<TSelf>
    where TSelf : class, IModelConfigurable<TSelf>
{
    /// <summary>
    /// Configures the EF Core mapping for <typeparamref name="TSelf"/>.
    /// </summary>
    static abstract void ConfigureModel(EntityTypeBuilder<TSelf> entity);
}
