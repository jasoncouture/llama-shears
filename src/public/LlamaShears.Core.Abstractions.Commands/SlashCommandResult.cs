namespace LlamaShears.Core.Abstractions.Commands;

/// <summary>
/// Outcome of an <see cref="ISlashCommand"/> execution. The flags here
/// are post-execution hints to the dispatcher (e.g. the chat UI) so it
/// can apply downstream effects without the command coupling to UI
/// concerns directly.
/// </summary>
/// <param name="ContextChanged">
/// <see langword="true"/> when the command mutated the agent's
/// conversation context (cleared, archived, etc.). Hosts that render
/// the conversation should refresh their view.
/// </param>
public sealed record SlashCommandResult(bool ContextChanged = false)
{
    /// <summary>Result with no post-execution side-effects.</summary>
    public static SlashCommandResult Default { get; } = new();

    /// <summary>Result signalling that the agent's context was modified.</summary>
    public static SlashCommandResult ContextWasChanged { get; } = new(ContextChanged: true);
}
