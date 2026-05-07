namespace LlamaShears.Core.Abstractions.SystemPrompt;

/// <summary>
/// A single workspace file surfaced to the system-prompt template
/// (<see cref="SystemPromptTemplateParameters.Files"/>) so the
/// template can fold its content directly into the prompt body.
/// </summary>
/// <param name="Name">File name relative to the workspace root.</param>
/// <param name="Content">File content as a string.</param>
public sealed record WorkspaceFile(string Name, string Content);
