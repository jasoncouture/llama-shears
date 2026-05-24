# LlamaShears.Core.Abstractions.PromptContext.AgentSkills

Assembly: `LlamaShears.Core.Abstractions`

Immutable snapshot of the skills exposed to a single agent for a single turn.
Produced by [ISkillRepository](ISkillRepository.md).`GetSkillsAsync` after filtering, and
surfaced through the prompt-context data bag under [AgentSkills](AgentSkills.md).`DataKey`.

## Parameters

- `Skills` — The filtered set of skills available to the agent.

## Fields

### `DataKey`

The well-known key used to publish this snapshot into the prompt-context data bag.

## Properties

### `Available`

Indicates whether at least one skill is present in the snapshot.

### `None`

An empty snapshot returned when skills are disabled or none are visible.

### `Skills`

The filtered set of skills available to the agent.

## Methods

### `AgentSkills`(ImmutableArray<[SkillRecord](SkillRecord.md)> Skills)

Immutable snapshot of the skills exposed to a single agent for a single turn.
Produced by [ISkillRepository](ISkillRepository.md).`GetSkillsAsync` after filtering, and
surfaced through the prompt-context data bag under [AgentSkills](AgentSkills.md).`DataKey`.

#### Parameters

- `Skills` — The filtered set of skills available to the agent.

