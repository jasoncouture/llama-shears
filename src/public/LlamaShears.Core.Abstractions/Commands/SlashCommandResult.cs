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
/// <param name="StreamingInterrupted">
/// <see langword="true"/> when the command stopped an in-flight turn
/// without changing persisted context. Hosts should close any open
/// streaming bubbles (assistant message / thought / in-flight tool)
/// for the active turn so the UI doesn't get stuck waiting for a
/// final fragment that will never arrive. The persistent
/// conversation history above the streaming bubble is preserved.
/// </param>
public sealed record SlashCommandResult(
    bool ContextChanged = false,
    bool StreamingInterrupted = false)
{
    /// <summary>Result with no post-execution side-effects.</summary>
    public static SlashCommandResult Default { get; } = new SlashCommandResult();

    /// <summary>Result signalling that the agent's context was modified.</summary>
    public static SlashCommandResult ContextWasChanged { get; } = new SlashCommandResult(ContextChanged: true);

    /// <summary>Result signalling that an in-flight turn was interrupted.</summary>
    public static SlashCommandResult StreamingWasInterrupted { get; } =
        new SlashCommandResult(StreamingInterrupted: true);
}
