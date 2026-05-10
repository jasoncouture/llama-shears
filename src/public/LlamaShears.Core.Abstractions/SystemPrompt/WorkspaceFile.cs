namespace LlamaShears.Core.Abstractions.SystemPrompt;

/// <summary>
/// In-memory representation of a single file that should land in an agent's
/// workspace overlay alongside the rendered system prompt.
/// </summary>
/// <param name="Name">Workspace-relative file name (including extension and any subdirectories).</param>
/// <param name="Content">UTF-8 text content of the file.</param>
public sealed record WorkspaceFile(string Name, string Content);
