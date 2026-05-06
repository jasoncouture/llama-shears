namespace LlamaShears.Core.Abstractions.PromptContext;

/// <summary>
/// Inputs the per-turn prompt-context template renders against. The
/// template (Scriban) decides how to format these into the
/// <c>&lt;system&gt;...&lt;/system&gt;</c> prefix; new fields are added
/// here rather than composed in C# so the template stays the single
/// point of authorship.
/// </summary>
/// <param name="Now">Current wall-clock time formatted by the host (string so the template renders verbatim).</param>
/// <param name="Timezone">Host timezone display name.</param>
/// <param name="DayOfWeek">Current day of week as text.</param>
/// <param name="ChannelId">Channel correlation id when the turn originates from a channel; <see langword="null"/> otherwise.</param>
/// <param name="ImportantMessage">Optional one-shot system message to surface (e.g. a warning).</param>
/// <param name="WorkspacePath">The agent's workspace path; <see langword="null"/> when unbound.</param>
public sealed record PromptContextParameters(
    string? Now = null,
    string? Timezone = null,
    string? DayOfWeek = null,
    string? ChannelId = null,
    string? ImportantMessage = null,
    string? WorkspacePath = null)
{
    /// <summary>Memory hits surfaced to the template (typically prefetch results).</summary>
    public IReadOnlyList<PromptContextMemory> Memories { get; init; } = [];
}
