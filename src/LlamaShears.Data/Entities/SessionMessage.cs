using LlamaShears.Data.Abstractions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LlamaShears.Data.Entities;

/// <summary>
/// A single entry in a <see cref="Session"/>'s LLM context.
/// Immutable once persisted: there is no <c>LastModified</c>, and the
/// id and created timestamp are locked by the data interceptors. The
/// reference to the owning <see cref="Session"/> is implicit in
/// <see cref="SessionId"/>; the relationship itself is configured on
/// the principal side (in <see cref="Session"/>).
/// </summary>
public class SessionMessage : IDataObject, ICreated, IModelConfigurable<SessionMessage>
{
    /// <inheritdoc />
    public Guid Id { get; init; }

    /// <inheritdoc />
    public DateTimeOffset Created { get; init; }

    /// <summary>
    /// Foreign key to the owning <see cref="Session"/>.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Role of the participant who produced the message.
    /// </summary>
    public SessionMessageRole Role { get; set; }

    /// <summary>
    /// Raw textual content of the message.
    /// </summary>
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
