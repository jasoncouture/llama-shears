using System.Globalization;

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

    public string Build(string agentId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        return string.Concat(
            Body,
            "\n\nCurrent UTC time: ",
            now.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
    }
}
