using LlamaShears.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LlamaShears.Data;

/// <summary>
/// EF Core context for LlamaShears persistent state. Per-entity
/// mappings are wired up by <see cref="ModelConfigurationExtensions"/>:
/// the dispatcher applies the conventions implied by
/// <c>IDataObject</c> / <c>IModifiableDataObject</c> and then invokes
/// each entity's own static <c>ConfigureModel</c>.
/// </summary>
public class LlamaShearsDbContext : DbContext
{
    public LlamaShearsDbContext(DbContextOptions<LlamaShearsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Agent> Agents => Set<Agent>();

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<SessionMessage> SessionMessages => Set<SessionMessage>();

    public DbSet<AgentSession> AgentSessions => Set<AgentSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureAllModels();
    }
}
