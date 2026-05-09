using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Core;

public sealed class InferenceRunner : IInferenceRunner
{
    private static readonly TimeSpan _interruptFinalizeTimeout = TimeSpan.FromSeconds(5);

    private readonly IEventPublisher _eventPublisher;
    private readonly IToolCallDispatcher _toolDispatcher;
    private readonly TimeProvider _time;
    private readonly IPromptContextProvider _promptContext;
    private readonly IMemorySearcher _memorySearcher;
    private readonly IAgentConfigProvider _agentConfigProvider;
    private readonly ICurrentAgentAccessor _currentAgent;

    public InferenceRunner(
        IEventPublisher eventPublisher,
        IToolCallDispatcher toolDispatcher,
        TimeProvider time,
        IPromptContextProvider promptContext,
        IMemorySearcher memorySearcher,
        IAgentConfigProvider agentConfigProvider,
        ICurrentAgentAccessor currentAgent)
    {
        _eventPublisher = eventPublisher;
        _toolDispatcher = toolDispatcher;
        _time = time;
        _promptContext = promptContext;
        _memorySearcher = memorySearcher;
        _agentConfigProvider = agentConfigProvider;
        _currentAgent = currentAgent;
    }

    public async Task<InferenceOutcome> RunAsync(
        string eventId,
        ILanguageModel model,
        ModelPrompt prompt,
        PromptOptions? options,
        bool emitTurns,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(prompt);
        if (string.IsNullOrWhiteSpace(_currentAgent.Current?.AgentId))
            throw new InvalidOperationException("Agent ID context is required, but is not present");

        if (prompt.Turns.Count == 0)
        {
            throw new ArgumentException("Prompt must contain at least one turn.", nameof(prompt));
        }

        CorrelationId.Value = correlationId;
        EventId.Value = eventId;
        ChannelId.Value = prompt.Turns[^1].ChannelId;

        if (options is { InjectEphemeralContext: true })
        {
            prompt = await InjectEphemeralAsync(prompt, cancellationToken).ConfigureAwait(false);
        }


        var thinking = new StringBuilder();
        var content = new StringBuilder();
        int? tokenCount = null;
        var toolCalls = ImmutableArray.CreateBuilder<ToolCall>();

        var tools = options?.Tools ?? [];
        var dispatchTasks = new List<Task<ToolCallResult>>();
        var interrupted = false;
        var pendingDispatchTasks = new List<Task<ToolCallResult>>();

        var errorStateCancellationToken = new CancellationTokenSource();

        try
        {
            await foreach (var fragment in model.PromptAsync(prompt, options, cancellationToken).ConfigureAwait(false))
            {
                switch (fragment)
                {
                    case IModelThoughtResponse thought:
                        thinking.Append(thought.Content);
                        await PublishModelFragment(ModelRole.Thought, new AgentThoughtFragment(thinking.ToString(), ChannelId: ChannelId.Value, Final: false), cancellationToken);
                        break;
                    case IModelTextResponse text:
                        content.Append(text.Content);
                        await PublishModelFragment(ModelRole.Assistant, new AgentMessageFragment(content.ToString(), ChannelId: ChannelId.Value, Final: false), cancellationToken);
                        break;
                    case IModelToolCallFragment toolFragment:
                        toolCalls.Add(toolFragment.Call);
                        var agentToolCallFragmnet = new AgentToolCallFragment(
                                toolFragment.Call.Source,
                                toolFragment.Call.Name,
                                toolFragment.Call.ArgumentsJson,
                                toolFragment.Call.CallId);
                        await PublishModelFragment(ModelRole.Tool, agentToolCallFragmnet, cancellationToken);
                        dispatchTasks.Add(_toolDispatcher.DispatchAsync(toolFragment.Call, tools, eventId, correlationId, cancellationToken).AsTask());
                        pendingDispatchTasks.Add(dispatchTasks[^1]);
                        break;
                    case IModelCompletionResponse completion:
                        tokenCount = completion.TokenCount;
                        break;
                }
                // If any tools are complete, publish their turns.
                await PublishAndRemoveCompletedToolCallsAsync(pendingDispatchTasks, dispatchTasks, toolCalls, cancellationToken);
            }
            // Wait for any dangling tasks, and publish them.
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            interrupted = true;
            cancellationToken = errorStateCancellationToken.Token;
            errorStateCancellationToken.CancelAfter(_interruptFinalizeTimeout);
        }
        finally
        {
            await Task.WhenAll(pendingDispatchTasks);
            await PublishAndRemoveCompletedToolCallsAsync(pendingDispatchTasks, dispatchTasks, toolCalls, cancellationToken);
            Debug.Assert(pendingDispatchTasks.Count == 0);
        }


        if (thinking.Length > 0)
        {
            await PublishModelFragment(ModelRole.Thought, new AgentThoughtFragment(thinking.ToString(), ChannelId: ChannelId.Value, Final: true), cancellationToken);
            if (emitTurns)
            {
                // I would refactor this too into a method, just like above.
                // but 2 instances isn't worth the effort.
                var thoughtTurn = new ModelTurn(ModelRole.Thought, thinking.ToString(), _time.GetLocalNow(), ChannelId: ChannelId.Value);
                await _eventPublisher.PublishAsync(
                    Event.WellKnown.Agent.Turn with { Id = eventId },
                    thoughtTurn,
                    correlationId,
                    cancellationToken);
            }
        }
        if (content.Length > 0)
        {
            await PublishModelFragment(ModelRole.Assistant, new AgentMessageFragment(content.ToString(), ChannelId: ChannelId.Value, Final: true), cancellationToken);
        }
        if (emitTurns && (content.Length > 0 || toolCalls.Count > 0))
        {
            var assistantTurn = new ModelTurn(ModelRole.Assistant, content.ToString(), _time.GetLocalNow(), ChannelId: ChannelId.Value)
            {
                ToolCalls = toolCalls.ToImmutable(),
            };
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = eventId },
                assistantTurn,
                correlationId,
                cancellationToken);
        }


        return new InferenceOutcome(
            thinking.ToString(),
            content.ToString(),
            tokenCount,
            toolCalls.ToImmutable(),
            [.. await Task.WhenAll(dispatchTasks)],
            Interrupted: interrupted);
    }

    private static AsyncLocal<Guid?> CorrelationId { get; } = new AsyncLocal<Guid?>();
    private static AsyncLocal<string?> EventId { get; } = new AsyncLocal<string?>();
    private static AsyncLocal<string?> ChannelId { get; } = new AsyncLocal<string?>();



    private async Task PublishModelFragment<T>(ModelRole role, T fragment, CancellationToken cancellationToken) where T : class, IAgentMessage
    {
        var correlationId = CorrelationId.Value ?? throw new InvalidOperationException("Correlation ID scope is not set");
        var eventId = EventId.Value ?? throw new InvalidOperationException("Event ID scope is not set");
        var typedEventId = role switch
        {
            ModelRole.Thought => Event.WellKnown.Agent.Thought,
            ModelRole.Assistant => Event.WellKnown.Agent.Message,
            ModelRole.Tool => Event.WellKnown.Agent.ToolCall,
            _ => throw new ArgumentException("Unknown model role", nameof(role))
        } with
        { Id = eventId };
        await _eventPublisher.PublishAsync(
            typedEventId,
            fragment,
            correlationId,
            cancellationToken);
    }

    private async ValueTask PublishAndRemoveCompletedToolCallsAsync(
        List<Task<ToolCallResult>> pending,
        List<Task<ToolCallResult>> allDispatchTasks,
        ImmutableArray<ToolCall>.Builder allToolCalls,
        CancellationToken cancellationToken)
    {
        var correlationId = CorrelationId.Value ?? throw new InvalidOperationException("Correlation ID scope is not set");
        var eventId = EventId.Value ?? throw new InvalidOperationException("Event ID scope is not set");
        while (true)
        {
            var pendingIndex = pending.FindIndex(t => t.IsCompleted);
            if (pendingIndex < 0)
            {
                return;
            }
            var task = pending[pendingIndex];
            pending.RemoveAt(pendingIndex);
            var result = await task;
            var toolTurn = new ModelTurn(ModelRole.Tool, result.Content, _time.GetLocalNow(), ChannelId: ChannelId.Value)
            {
                ToolCall = allToolCalls[allDispatchTasks.IndexOf(task)],
                IsError = result.IsError,
            };
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = eventId },
                toolTurn,
                correlationId,
                cancellationToken);
        }
    }

    private async Task<ModelPrompt> InjectEphemeralAsync(ModelPrompt prompt, CancellationToken cancellationToken)
    {
        var agentId = _currentAgent.Current?.AgentId!;
        var config = await _agentConfigProvider.GetConfigAsync(agentId, cancellationToken).ConfigureAwait(false);
        if (config is null)
        {
            return prompt;
        }

        var memories = await SearchMemoriesAsync(agentId, GetMemorySearchQueries(prompt.Turns), cancellationToken).ConfigureAwait(false);

        var now = _time.GetLocalNow();
        var parameters = new PromptContextParameters(
            Now: now,
            Timezone: TimeZoneInfo.Local.Id,
            DayOfWeek: now.DayOfWeek.ToString(),
            ChannelId: ChannelId.Value,
            ImportantMessage: null,
            WorkspacePath: config.WorkspacePath)
        {
            Memories = memories,
        };
        var body = await _promptContext.GetAsync(config.PromptContext, parameters, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body))
        {
            return prompt;
        }

        var ephemeral = new ModelTurn(ModelRole.SystemEphemeral, body, now, Ephemeral: true);
        return new ModelPrompt(InsertAfterLastNonUser(prompt.Turns, ephemeral));
    }

    private int GetLastUserMessageIndex(IEnumerable<ModelTurn> turns)
    {
        return turns.Select((item, index) => (item, index))
            .Reverse()
            .TakeWhile(i => i.item.Role == ModelRole.User).Select(i => i.index)
            .Cast<int>()
            .DefaultIfEmpty(0)
            .Last();
    }

    // returns turns in reverse order, which doesn't matter because this is intended for memory searches, and order does not matter.
    private IEnumerable<string> GetMemorySearchQueries(IEnumerable<ModelTurn> turns)
    {
        var turnArray = turns.ToArray();

        return turns.Reverse().Aggregate(new PromptSearchState(false, false, []), AggregateMemoryMessages, (state) => state.Turns.Select(i => i.Content));

        PromptSearchState AggregateMemoryMessages(PromptSearchState state, ModelTurn turn)
        {
            if (state.Complete) return state;
            if (state.UserMessageSeen && turn.Role != ModelRole.User)
            {
                return state with { Complete = true };
            }
            if (turn.Role == ModelRole.User)
            {
                state = state with { UserMessageSeen = true };
            }

            return state with { Turns = [.. state.Turns, turn] };
        }
    }

    private record struct PromptSearchState(bool UserMessageSeen, bool Complete, ImmutableArray<ModelTurn> Turns);

    private ImmutableArray<ModelTurn> InsertAfterLastNonUser(IReadOnlyList<ModelTurn> turns, ModelTurn ephemeral)
    {
        var insertAt = GetLastUserMessageIndex(turns);


        var preUserTurns = turns.Take(insertAt);
        var postUserTurns = turns.Skip(insertAt);
        return [.. preUserTurns, ephemeral, .. postUserTurns];
    }

    private async ValueTask<IReadOnlyList<PromptContextMemory>> SearchMemoriesAsync(
        string agentId,
        IEnumerable<string> queries,
        CancellationToken cancellationToken)
    {
        var results = new List<MemorySearchResult>();
        foreach (var query in queries)
        {
            results.AddRange(await _memorySearcher.SearchAsync(agentId, query, limit: null, minScore: null, cancellationToken));
        }

        return [.. results
            .Select(static i => new PromptContextMemory(i.RelativePath, i.Summary, i.Score))
            .OrderByDescending(i => i.Score)
            .DistinctBy(i => i.RelativePath)];
    }
}
