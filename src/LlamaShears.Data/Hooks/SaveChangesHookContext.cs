using Microsoft.EntityFrameworkCore;

namespace LlamaShears.Data.Hooks;

/// <summary>
/// Per-save state shared with every <see cref="ISaveChangesHook"/>
/// invocation during a single <c>SaveChanges</c> call. A fresh
/// instance is constructed by the interceptor at the start of each
/// save, so all hooks see the same <see cref="UtcNow"/> snapshot.
/// </summary>
public sealed class SaveChangesHookContext
{
    /// <summary>
    /// The <see cref="DbContext"/> the save is running against.
    /// </summary>
    public required DbContext DbContext { get; init; }

    /// <summary>
    /// A single <c>UtcNow</c> snapshot taken at the start of the save.
    /// Hooks that need a "current time" should use this rather than
    /// calling <see cref="DateTimeOffset.UtcNow"/> directly, so all
    /// timestamps written within one save are consistent.
    /// </summary>
    public required DateTimeOffset UtcNow { get; init; }
}
