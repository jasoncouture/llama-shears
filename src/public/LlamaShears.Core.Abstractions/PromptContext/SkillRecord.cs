using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.PromptContext;

/// <summary>
/// Materialised skill loaded from a <c>SKILL.md</c> document. Captures the
/// frontmatter contract (name, description, optional metadata, free-form extras)
/// together with the resolved on-disk path and the rendered markdown body.
/// </summary>
/// <param name="Name">The skill's declared name from the <c>name</c> frontmatter field; used as the primary identifier when the agent invokes the skill.</param>
/// <param name="Description">Human/agent-facing description from the <c>description</c> frontmatter field; surfaced in the catalog the agent uses to choose a skill.</param>
/// <param name="Path">Absolute path to the <c>SKILL.md</c> file the record was loaded from. The containing directory is the skill's resource root for sibling files referenced by <see cref="Body"/>.</param>
/// <param name="Body">Markdown body of the skill document with the YAML frontmatter stripped. Returned verbatim when the agent loads the skill.</param>
/// <param name="ExtraProperties">Frontmatter fields outside the reserved <c>name</c>, <c>description</c>, and <c>metadata</c> keys, preserved verbatim for downstream consumers.</param>
/// <param name="Metadata">Contents of the optional <c>metadata</c> frontmatter object, or an empty dictionary when the field is absent.</param>
public record SkillRecord(
    string Name,
    string Description,
    string Path,
    string Body,
    ImmutableDictionary<string, object?> ExtraProperties,
    ImmutableDictionary<string, object?> Metadata);
