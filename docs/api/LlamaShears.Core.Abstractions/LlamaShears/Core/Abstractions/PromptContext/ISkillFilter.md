# LlamaShears.Core.Abstractions.PromptContext.ISkillFilter

Assembly: `LlamaShears.Core.Abstractions`

Gatekeeps skill availability for the current agent context. The repository
consults the filter before enumerating skills ([ISkillFilter](ISkillFilter.md).`AreSkillsAllowed`)
and again per skill ([ISkillFilter](ISkillFilter.md).`IsSkillAllowed`) so policies can be applied
globally or per name.

## Methods

### `AreSkillsAllowed`

Indicates whether skills are enabled at all for the current context.
Returning `false` short-circuits enumeration and yields an empty result.

#### Returns

`true` when skills should be exposed; otherwise `false`.

### `IsSkillAllowed`(string skillName)

Indicates whether a specific skill is permitted in the current context.
Invoked once per discovered skill after [ISkillFilter](ISkillFilter.md).`AreSkillsAllowed` has returned `true`.

#### Parameters

- `skillName` — Name of the skill as declared in its `SKILL.md` frontmatter.

#### Returns

`true` when the named skill should be exposed; otherwise `false`.

