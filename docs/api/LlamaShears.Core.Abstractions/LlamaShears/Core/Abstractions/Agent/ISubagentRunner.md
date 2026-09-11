# LlamaShears.Core.Abstractions.Agent.ISubagentRunner

Assembly: `LlamaShears.Core.Abstractions`

Spawns a one-shot transient child of the ambient root session
(`subagent_run`). Nested children are refused.

## Methods

### `RunAsync`([SubagentRunRequest](SubagentRunRequest.md) request, CancellationToken cancellationToken)

Starts a child session for `request`. When
[SubagentRunRequest](SubagentRunRequest.md).`AwaitResult` is
`true`, waits for the child to idle (or time
out) and returns the last assistant text. When
`false`, returns as soon as the child is
started ([SubagentRunResult](SubagentRunResult.md).`Ok` is still
`true`); the child reports later via the
parent channel or `session_send`.

