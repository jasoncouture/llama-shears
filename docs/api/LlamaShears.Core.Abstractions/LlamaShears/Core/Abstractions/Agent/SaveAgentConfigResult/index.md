# LlamaShears.Core.Abstractions.Agent.SaveAgentConfigResult

## Types

- [Conflict](Conflict.md) — Expected hash didn't match the on-disk hash; nothing was written.
- [InvalidJson](InvalidJson.md) — Content failed to deserialize into [AgentConfig](../AgentConfig.md); nothing was written.
- [Ok](Ok.md) — Save succeeded; `NewHash` is the post-write digest.

