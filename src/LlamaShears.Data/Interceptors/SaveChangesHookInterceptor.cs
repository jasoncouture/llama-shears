using LlamaShears.Data.Hooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LlamaShears.Data.Interceptors;

/// <summary>
/// The single EF Core save-changes interceptor for the LlamaShears
/// data layer. Performs one forward pass over the change tracker for
/// each <c>SaveChanges</c> call, invoking every registered
/// <see cref="ISaveChangesHook"/> against each entity in the Added,
/// Modified, or Deleted state. A shared
/// <see cref="SaveChangesHookContext"/> (with a single UtcNow
/// snapshot) is passed to every hook. Hook execution order is
/// undefined and must not be relied on by hook authors — see
/// <see cref="ISaveChangesHook"/>.
/// </summary>
public sealed class SaveChangesHookInterceptor : SaveChangesInterceptor
{
    private readonly IReadOnlyList<ISaveChangesHook> hooks;

    public SaveChangesHookInterceptor(IEnumerable<ISaveChangesHook> hooks)
    {
        ArgumentNullException.ThrowIfNull(hooks);
        this.hooks = hooks.ToArray();
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? dbContext)
    {
        if (dbContext is null || hooks.Count == 0)
        {
            return;
        }

        var hookContext = new SaveChangesHookContext
        {
            DbContext = dbContext,
            UtcNow = DateTimeOffset.UtcNow,
        };

        var pending = dbContext.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        foreach (var entry in pending)
        {
            foreach (var hook in hooks)
            {
                hook.Apply(entry, hookContext);
            }
        }
    }
}
