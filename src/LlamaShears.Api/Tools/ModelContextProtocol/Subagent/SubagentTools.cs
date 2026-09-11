using System.ComponentModel;
using LlamaShears.Core.Abstractions.Agent;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Subagent;

[McpServerToolType]
public sealed class SubagentTools
{
    private readonly ISubagentRunner _runner;

    public SubagentTools(ISubagentRunner runner)
    {
        _runner = runner;
    }

    [McpServerTool(Name = "subagent_run", Destructive = false, OpenWorld = false)]
    [Description("Spawns a one-shot child session of the calling root agent to do a bounded task. The child shares this workspace, memory, todos, and MCP allowlist — it is not a sandbox. Nested calls from an already-spawned child are refused. Pass prompt as the instruction. Optional context is prepended as a preamble. Optional model is a provider/model identity (not a URL). Optional maxTurns (1–64) caps the child's tool loop; omit to inherit the parent's unlimited budget. awaitResult defaults to true: wait for the child to idle and return its last assistant text in output (that path does not also enqueue a parent chat message). awaitResult=false returns immediately after spawn; the child later reports via session_send or a parent ChannelMessage. timeoutSeconds defaults to 120 (1–600); on timeout the child is stopped and timedOut is true. Result: ok is true when the requested mode succeeded (awaited idle, or fire-and-forget spawn). awaited is true only if you waited. error is refuse/spawn/timeout, not the child's own status.")]
    public ValueTask<SubagentRunResult> RunSubagent(
        [Description("Instruction for the child. Delivered as its first user-role turn.")]
        string prompt,
        [Description("Optional provider/model identity to overlay on the child (e.g. ollama/llama3). Not a URL.")]
        string? model = null,
        [Description("Optional preamble prepended to prompt as one user turn.")]
        string? context = null,
        [Description("Optional child tool-loop cap (1–64). Omit to keep the parent's Tools.TurnLimit.")]
        int? maxTurns = null,
        [Description("When true (default), wait for the child to finish and return its last assistant text. When false, return after spawn.")]
        bool awaitResult = true,
        [Description("Await ceiling in seconds when awaitResult is true. Defaults to 120 (min 1, max 600).")]
        int timeoutSeconds = SubagentRunRequest.DefaultTimeoutSeconds,
        CancellationToken cancellationToken = default)
        => _runner.RunAsync(
            new SubagentRunRequest(
                Prompt: prompt,
                Model: model,
                Context: context,
                MaxTurns: maxTurns,
                AwaitResult: awaitResult,
                TimeoutSeconds: timeoutSeconds),
            cancellationToken);
}
