using System.Globalization;

namespace LlamaShears.Agent.Core.SystemPrompt;

/// <summary>
/// Returns a constant system-prompt body with the supplied timestamp
/// appended at the end. This is the placeholder shipped while the
/// template/file-based prompt builder is being developed; once the
/// template path lands this implementation is replaced wholesale.
/// </summary>
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
