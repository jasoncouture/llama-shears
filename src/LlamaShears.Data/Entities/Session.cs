using LlamaShears.Data.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LlamaShears.Data.Entities;

public class Session : IDataObject, ICreated, ILastModified, IModelConfigurable<Session>
{
    /// <inheritdoc />
    public Guid Id { get; init; }

    /// <inheritdoc />
    public DateTimeOffset Created { get; init; }

    /// <inheritdoc />
    public DateTimeOffset LastModified { get; set; }

    /// <inheritdoc />
    public static void ConfigureModel(EntityTypeBuilder<Session> entity)
    {
        entity.HasMany<SessionMessage>()
            .WithOne()
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
