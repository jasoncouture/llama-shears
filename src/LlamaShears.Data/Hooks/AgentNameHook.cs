using System.Text.RegularExpressions;
using LlamaShears.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LlamaShears.Data.Hooks;

/// <summary>
/// Enforces the <see cref="Agent.Name"/> invariants at save time.
/// <list type="bullet">
///   <item>On add: the value must match
///   <c>^[a-z][a-z0-9]*$</c> exactly. Non-canonical input — including
///   anything containing uppercase letters — is rejected with
///   <see cref="InvalidOperationException"/>. The hook does
///   <b>not</b> rewrite the value; satisfying the contract is the
///   caller's responsibility.</item>
///   <item>On update: throw if <see cref="Agent.Name"/> has been
///   marked modified. <see cref="Agent.Name"/> is set-once.</item>
/// </list>
/// No-op for non-<see cref="Agent"/> entities and for the deleted
/// state.
/// </summary>
public sealed partial class AgentNameHook : ISaveChangesHook
{
    /// <summary>
    /// Canonical name format: a single lowercase ASCII letter
    /// followed by zero or more lowercase ASCII letters or digits.
    /// </summary>
    [GeneratedRegex("^[a-z][a-z0-9]*$")]
    private static partial Regex GetValidationRegex();

    public void Apply(EntityEntry entry, SaveChangesHookContext context)
    {
        if (entry.Entity is not Agent agent)
        {
            return;
        }

        if (entry.State is not (EntityState.Added or EntityState.Modified))
        {
            return;
        }

        if (entry.State is EntityState.Modified)
        {
            if (entry.Property(nameof(Agent.Name)).IsModified)
            {
                throw new InvalidOperationException(
                    $"Agent name on entity '{entry.Metadata.Name}' was modified after creation. " +
                    "Agent names are immutable.");
            }
            return;
        }

        if (!GetValidationRegex().IsMatch(agent.Name))
        {
            throw new InvalidOperationException("Invalid agent name.")
            {
                Data = { ["agentName"] = agent.Name },
            };
        }
    }
}
