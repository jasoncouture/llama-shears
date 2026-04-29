using LlamaShears.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LlamaShears.Data;

public class LlamaShearsDbContext : DbContext
{
    public LlamaShearsDbContext(DbContextOptions<LlamaShearsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<SessionMessage> SessionMessages => Set<SessionMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureAllModels();
    }
}
