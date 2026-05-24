# LlamaShears.Core.Abstractions.PromptContext.ISkillParser

Assembly: `LlamaShears.Core.Abstractions`

Parses the raw text of a `SKILL.md` document into a [SkillRecord](SkillRecord.md).
Implementations are pure CPU work — markdown + YAML frontmatter parsing —
and do not perform I/O. Returns `null` when the document
has no frontmatter; throws when the frontmatter is present but malformed
(missing required keys, wrong value types, etc.) so the caller can log and
skip the offending file.

## Methods

### `Parse`(string documentText, string filePath)

Parses the supplied document text and returns a populated [SkillRecord](SkillRecord.md).

#### Parameters

- `documentText` — Full raw markdown text of the `SKILL.md` file, including the YAML frontmatter block.
- `filePath` — Absolute path to the source `SKILL.md` file; stored on [SkillRecord](SkillRecord.md).`Path` and used to derive the skill's resource directory.

#### Returns

A populated [SkillRecord](SkillRecord.md), or `null` when `documentText` contains no YAML frontmatter block.

#### Exceptions

- InvalidOperationException — Thrown when the frontmatter is present but invalid — missing `name`/`description`, non-string values, or a non-object `metadata` field.

