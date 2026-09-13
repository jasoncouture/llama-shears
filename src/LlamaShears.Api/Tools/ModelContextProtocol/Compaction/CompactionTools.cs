using System.ComponentModel;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Compaction;

[McpServerToolType]
public sealed class CompactionTools
{
    private const string CompactionTemplateFileName = "COMPACTION.md";
    private const int SummarizerMaxTurns = 5;

    private readonly IDataContextScope _dataScope;
    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IContextCompactor _compactor;
    private readonly ISubagentRunner _subagentRunner;
    private readonly IEventBus _eventBus;

    public CompactionTools(
        IDataContextScope dataScope,
        IAgentContextProvider agentContextProvider,
        IContextCompactor compactor,
        ISubagentRunner subagentRunner,
        IEventBus eventBus)
    {
        _dataScope = dataScope;
        _agentContextProvider = agentContextProvider;
        _compactor = compactor;
        _subagentRunner = subagentRunner;
        _eventBus = eventBus;
    }

    [McpServerTool(Name = "context_compact", Destructive = true, OpenWorld = false)]
    [Description("Summarizes older conversation turns now, before the context window is full. Archives the live log and keeps the last six eligible turns plus a summary. Use when this session is getting long and you still need room to work. Does not undo. Returns a JSON object with compacted (true when a summary was written), preservedTurns (eligible turns kept after the summary), and error when the call was refused or the summarizer failed. compacted=false with no error means there were not enough turns to compact.")]
    public async Task<ContextCompactResult> CompactContext(CancellationToken cancellationToken = default)
    {
        if (_dataScope.TryGetAgentConfig() is null)
        {
            return new ContextCompactResult(
                Compacted: false,
                Error: "Refused: context_compact requires an authenticated agent on the request.");
        }

        var session = _dataScope.GetCurrentSessionId();
        var snapshot = await _agentContextProvider
            .CreateAgentContextAsync(session, cancellationToken)
            .ConfigureAwait(false);
        if (snapshot is null)
        {
            return new ContextCompactResult(
                Compacted: false,
                Error: "Refused: no agent context is available for this session.");
        }

        var prompt = new ModelPrompt([.. snapshot.LanguageModel.Turns]);
        var plan = await _compactor.TryPrepareAsync(snapshot, prompt, force: true, cancellationToken);
        if (plan is null)
        {
            return new ContextCompactResult(Compacted: false);
        }

        await _eventBus.PublishAsync(
            Event.WellKnown.Agent.CompactingStarted with { Id = session },
            new AgentCompactionRequest(Force: true),
            cancellationToken);
        try
        {
            var result = await _subagentRunner.RunAsync(
                new SubagentRunRequest(
                    Prompt: plan.Transcript.Content,
                    MaxTurns: SummarizerMaxTurns,
                    AwaitResult: true,
                    SystemPrompt: CompactionTemplateFileName,
                    PromptContext: CompactionTemplateFileName),
                cancellationToken);
            if (!result.Ok)
            {
                return new ContextCompactResult(
                    Compacted: false,
                    Error: result.Error ?? "Compaction summarizer failed before producing a summary.");
            }

            var summary = result.Output?.Trim() ?? "";
            if (summary.Length == 0)
            {
                return new ContextCompactResult(
                    Compacted: false,
                    Error: "Model produced an empty summary; cannot compact context.");
            }

            await _compactor.CommitAsync(plan, summary, cancellationToken);
            return new ContextCompactResult(
                Compacted: true,
                PreservedTurns: plan.Preserved.Length);
        }
        finally
        {
            await _eventBus.PublishAsync(
                Event.WellKnown.Agent.CompactingFinished with { Id = session },
                new AgentCompactionRequest(Force: true),
                CancellationToken.None);
        }
    }
}
