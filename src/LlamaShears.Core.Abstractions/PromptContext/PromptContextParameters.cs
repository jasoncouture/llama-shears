namespace LlamaShears.Core.Abstractions.PromptContext;

public sealed record PromptContextParameters(
    string? Now = null,
    string? Timezone = null,
    string? DayOfWeek = null,
    string? ChannelId = null,
    string? ImportantMessage = null)
{
    public IReadOnlyList<PromptContextMemory> Memories { get; init; } = [];
}
