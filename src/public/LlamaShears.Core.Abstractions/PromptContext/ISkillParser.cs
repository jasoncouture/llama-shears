namespace LlamaShears.Core.Abstractions.PromptContext;

/// <summary>
/// Parses the raw text of a <c>SKILL.md</c> document into a <see cref="SkillRecord"/>.
/// Implementations are pure CPU work — markdown + YAML frontmatter parsing —
/// and do not perform I/O. Returns <see langword="null"/> when the document
/// has no frontmatter; throws when the frontmatter is present but malformed
/// (missing required keys, wrong value types, etc.) so the caller can log and
/// skip the offending file.
/// </summary>
public interface ISkillParser
{
    /// <summary>
    /// Parses the supplied document text and returns a populated <see cref="SkillRecord"/>.
    /// </summary>
    /// <param name="documentText">Full raw markdown text of the <c>SKILL.md</c> file, including the YAML frontmatter block.</param>
    /// <param name="filePath">Absolute path to the source <c>SKILL.md</c> file; stored on <see cref="SkillRecord.Path"/> and used to derive the skill's resource directory.</param>
    /// <returns>A populated <see cref="SkillRecord"/>, or <see langword="null"/> when <paramref name="documentText"/> contains no YAML frontmatter block.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when the frontmatter is present but invalid — missing <c>name</c>/<c>description</c>, non-string values, or a non-object <c>metadata</c> field.</exception>
    SkillRecord? Parse(string documentText, string filePath);
}
