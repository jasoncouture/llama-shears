namespace LlamaShears.Core.SystemPrompt;

/// <summary>
/// Builds the system-prompt body that the agent prepends to every
/// prompt cycle. The current implementation is a hard-coded prompt
/// with the supplied timestamp appended; this interface is the seam
/// a future template/file-based prompt builder will replace.
/// </summary>
public interface ISystemPromptProvider
{
    string Build(string agentId, DateTimeOffset now);
}
