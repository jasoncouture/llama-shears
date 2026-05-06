using LlamaShears.Data.Abstractions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LlamaShears.Data.Entities;

public class SessionMessage : IDataObject, ICreated, IModelConfigurable<SessionMessage>
{
    /// <inheritdoc />
    public Guid Id { get; init; }

    /// <inheritdoc />
    public DateTimeOffset Created { get; init; }

    public Guid SessionId { get; set; }

    public SessionMessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <inheritdoc />
    public static void ConfigureModel(EntityTypeBuilder<SessionMessage> entity)
    {
        entity.Property(m => m.Role)
            .HasConversion<string>()
            .IsRequired();
        entity.Property(m => m.Content).IsRequired();
        entity.HasIndex(m => new { m.SessionId, m.Created });
    }
}
