namespace LlamaShears.Core.SystemPrompt;

public sealed record RuntimeContext
{
    public required string Line { get; init; }

    public required ReasoningLevel ReasoningLevel { get; init; }

    public required TimeZoneInfo Timezone { get; init; }
}
