using LlamaShears.Data.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LlamaShears.Data.Entities;

/// <summary>
/// An agent session — the persistent header for an LLM conversation
/// context. Inbound relationships are declared here on the referenced
/// (principal) side: by convention, every entity that references a
/// <see cref="Session"/> appears in <see cref="ConfigureModel"/>, so
/// the full picture of "who points at me" lives in one place.
/// </summary>
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

        entity.HasMany<AgentSession>()
            .WithOne()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
