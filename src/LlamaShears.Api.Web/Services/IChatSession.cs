using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Content;

namespace LlamaShears.Api.Web.Services;

/// <summary>
/// Per-circuit view of the active chat: which agent is selected,
/// what bubbles have arrived, and the user's display preferences
/// for thoughts and streaming output. Components react to
/// <see cref="Changed"/> rather than polling.
/// </summary>
public interface IChatSession : IDisposable
{
    /// <summary>Currently selected agent's id, or null if none.</summary>
    string? SelectedAgentId { get; }

    /// <summary>Snapshot of the displayed conversation bubbles.</summary>
    IReadOnlyList<ChatBubble> Bubbles { get; }

    /// <summary>
    /// Whether thought bubbles are visible. Setting fires
    /// <see cref="Changed"/>.
    /// </summary>
    bool ShowThoughts { get; set; }

    /// <summary>
    /// Whether in-flight (streaming) bubbles are visible. Setting
    /// fires <see cref="Changed"/>.
    /// </summary>
    bool ShowStreaming { get; set; }

    /// <summary>
    /// Whether tool-call activity (in-flight summary bubble and
    /// per-call result bubbles) is rendered with detail. When off,
    /// dispatch still happens; the UI shows a generic busy
    /// placeholder while tools run. Setting fires <see cref="Changed"/>.
    /// </summary>
    bool ShowTools { get; set; }

    /// <summary>
    /// Raised when any of the above observable state changes.
    /// </summary>
    event Action? Changed;

    /// <summary>
    /// Switches the active agent (or clears it when null/empty),
    /// loading and replaying any persisted history.
    /// </summary>
    Task SelectAgentAsync(string? agentId, CancellationToken cancellationToken);

    /// <summary>
    /// Sends user input to the selected agent. Recognized slash
    /// commands (e.g. <c>/clear</c>, <c>/archive</c>) execute against
    /// the agent's stored context instead of being delivered as a
    /// turn.
    /// </summary>
    Task SendAsync(string content, CancellationToken cancellationToken);

    /// <summary>
    /// Sends user input plus attachments (images today; future kinds
    /// land alongside) to the selected agent. Empty
    /// <paramref name="attachments"/> behaves identically to the
    /// text-only overload.
    /// </summary>
    Task SendAsync(string content, ImmutableArray<Attachment> attachments, CancellationToken cancellationToken);
}
