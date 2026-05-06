using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LlamaShears.Data.Hooks;

/// <summary>
/// A per-entity hook invoked once for each tracked entity in
/// <see cref="Microsoft.EntityFrameworkCore.EntityState.Added"/>,
/// <see cref="Microsoft.EntityFrameworkCore.EntityState.Modified"/>, or
/// <see cref="Microsoft.EntityFrameworkCore.EntityState.Deleted"/>
/// state during a single <c>SaveChanges</c> call.
/// <para>
/// Hook execution order is **undefined** and must not be relied on.
/// If two hooks require a defined sequence, they belong combined into
/// a single hook. Each hook should operate on a disjoint slice of
/// entity state. If a hook throws, the save is aborted by the
/// surrounding interceptor.
/// </para>
/// </summary>
public interface ISaveChangesHook
{
    /// <summary>
    /// Apply this hook to a single tracked entity. Implementations
    /// should no-op for entities or states they do not care about.
    /// </summary>
    void Apply(EntityEntry entry, SaveChangesHookContext context);
}
