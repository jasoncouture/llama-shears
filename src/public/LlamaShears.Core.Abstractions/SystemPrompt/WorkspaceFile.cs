namespace LlamaShears.Core.Abstractions.SystemPrompt;

/// <summary>
/// In-memory representation of a single file that should land in an agent's
/// workspace overlay alongside the rendered system prompt.
/// </summary>
/// <param name="Name">Leaf file name (e.g. <c>AGENTS.md</c>).</param>
/// <param name="Path">Absolute directory the file lives in, terminated by the platform directory separator (e.g. <c>/home/user/.llama-shears/workspace/alpha/</c>). Concatenating <paramref name="Path"/> + <paramref name="Name"/> yields the file's absolute path.</param>
/// <param name="Content">UTF-8 text content of the file.</param>
public sealed record WorkspaceFile(string Name, string Path, string Content);
