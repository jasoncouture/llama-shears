using System.Globalization;
using LlamaShears.Core.Abstractions.SystemPrompt;

namespace LlamaShears.Core.SystemPrompt;

public sealed class HardcodedSystemPromptProvider : ISystemPromptProvider
{
    private const string Body =
        "You are an assistant running inside LlamaShears. " +
        "Respond directly to the user's most recent message. " +
        "If multiple user messages have been combined into a single turn, " +
        "address them in order. " +
        "Do not invent tools, capabilities, or context that have not been " +
        "explicitly described to you.";

    private readonly TimeProvider _timeProvider;

    public HardcodedSystemPromptProvider(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    public string Build(string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        return string.Concat(
            Body,
            "\n\nCurrent UTC time: ",
            _timeProvider.GetUtcNow().ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
    }
}
