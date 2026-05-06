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
    public required DbContext DbContext { get; init; }

    /// <summary>
    /// A single <c>UtcNow</c> snapshot taken at the start of the save,
    /// shared by every hook so timestamps within one save are consistent.
    /// </summary>
    public required DateTimeOffset UtcNow { get; init; }
}
