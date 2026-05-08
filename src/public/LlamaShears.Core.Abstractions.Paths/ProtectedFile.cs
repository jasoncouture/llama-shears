namespace LlamaShears.Core.Abstractions.Paths;

/// <summary>
/// Declares a protection rule for paths inside an agent workspace.
/// </summary>
/// <param name="Glob">
/// FileSystemGlobbing-style glob anchored at the workspace root
/// (e.g. <c>.git/**</c>, <c>*.md</c>, <c>**/.git</c>). Matched
/// case-insensitively against forward-slash relative paths.
/// </param>
/// <param name="ProtectionMode">Modes denied for matched paths.</param>
/// <param name="Type">Filesystem entry kinds the rule applies to.</param>
/// <param name="Reason">Optional explanation surfaced in refusal messages.</param>
public sealed record ProtectedFile(string Glob, ProtectionMode ProtectionMode, FileType Type, string? Reason = null);
