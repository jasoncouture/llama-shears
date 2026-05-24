# LlamaShears.Core.Abstractions.PromptContext.ISkillRepository

Assembly: `LlamaShears.Core.Abstractions`

Loads the set of skills visible to a particular agent, applying any configured
[ISkillFilter](ISkillFilter.md) before the result is returned. Skills are discovered
on disk under the host-defined skill roots (global, app, and per-agent workspace
tiers) and represented as [SkillRecord](SkillRecord.md) values.

## Methods

### `GetSkillsAsync`(string agentId, CancellationToken cancellationToken)

Returns the skills available to the specified agent after security filters
are applied. The result is an immutable snapshot; callers should treat it
as the authoritative view for the current turn.

#### Parameters

- `agentId` — Identifier of the agent whose skills should be enumerated. Used to resolve the per-agent skill root.
- `cancellationToken` — Cancellation token that aborts the enumeration.

#### Returns

An [AgentSkills](AgentSkills.md) snapshot containing the filtered set of skills, or [AgentSkills](AgentSkills.md).`None` when none are available.

