# LlamaShears.Core.Abstractions.PromptContext.SkillRecord

Assembly: `LlamaShears.Core.Abstractions`

Materialised skill loaded from a `SKILL.md` document. Captures the
frontmatter contract (name, description, optional metadata, free-form extras)
together with the resolved on-disk path and the rendered markdown body.

## Parameters

- `Name` — The skill's declared name from the `name` frontmatter field; used as the primary identifier when the agent invokes the skill.
- `Description` — Human/agent-facing description from the `description` frontmatter field; surfaced in the catalog the agent uses to choose a skill.
- `Path` — Absolute path to the `SKILL.md` file the record was loaded from. The containing directory is the skill's resource root for sibling files referenced by [SkillRecord](SkillRecord.md).`Body`.
- `Body` — Markdown body of the skill document with the YAML frontmatter stripped. Returned verbatim when the agent loads the skill.
- `ExtraProperties` — Frontmatter fields outside the reserved `name`, `description`, and `metadata` keys, preserved verbatim for downstream consumers.
- `Metadata` — Contents of the optional `metadata` frontmatter object, or an empty dictionary when the field is absent.

## Properties

### `Body`

Markdown body of the skill document with the YAML frontmatter stripped. Returned verbatim when the agent loads the skill.

### `Description`

Human/agent-facing description from the `description` frontmatter field; surfaced in the catalog the agent uses to choose a skill.

### `ExtraProperties`

Frontmatter fields outside the reserved `name`, `description`, and `metadata` keys, preserved verbatim for downstream consumers.

### `Metadata`

Contents of the optional `metadata` frontmatter object, or an empty dictionary when the field is absent.

### `Name`

The skill's declared name from the `name` frontmatter field; used as the primary identifier when the agent invokes the skill.

### `Path`

Absolute path to the `SKILL.md` file the record was loaded from. The containing directory is the skill's resource root for sibling files referenced by [SkillRecord](SkillRecord.md).`Body`.

## Methods

### `SkillRecord`(string Name, string Description, string Path, string Body, ImmutableDictionary<string, object> ExtraProperties, ImmutableDictionary<string, object> Metadata)

Materialised skill loaded from a `SKILL.md` document. Captures the
frontmatter contract (name, description, optional metadata, free-form extras)
together with the resolved on-disk path and the rendered markdown body.

#### Parameters

- `Name` — The skill's declared name from the `name` frontmatter field; used as the primary identifier when the agent invokes the skill.
- `Description` — Human/agent-facing description from the `description` frontmatter field; surfaced in the catalog the agent uses to choose a skill.
- `Path` — Absolute path to the `SKILL.md` file the record was loaded from. The containing directory is the skill's resource root for sibling files referenced by [SkillRecord](SkillRecord.md).`Body`.
- `Body` — Markdown body of the skill document with the YAML frontmatter stripped. Returned verbatim when the agent loads the skill.
- `ExtraProperties` — Frontmatter fields outside the reserved `name`, `description`, and `metadata` keys, preserved verbatim for downstream consumers.
- `Metadata` — Contents of the optional `metadata` frontmatter object, or an empty dictionary when the field is absent.

