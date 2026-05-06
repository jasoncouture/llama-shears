# Tool calling

How an agent's model invokes external work. The contract surface is in [`Core.Abstractions/Provider/`](../../src/LlamaShears.Core.Abstractions/Provider/) (`ToolCall`, `ToolGroup`, `ToolDescriptor`, `ToolParameter`); the dispatch path is in [`Core/Tools/ModelContextProtocol/`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/); the bundled tools live in [`Api/Tools/ModelContextProtocol/`](../../src/LlamaShears.Api/Tools/ModelContextProtocol/).

## Goals

- A single tool-calling abstraction that works equally well for tools hosted by the host's own MCP listener and tools exposed by remote MCP servers. MCP is the only mechanism — there's no separate "in-process direct-call" path.
- A turn model that represents text, thoughts, tool-call requests, and tool results without polymorphic-class inheritance.
- A streaming wire format that can carry tool-call deltas as the model produces them.
- A deterministic agent loop with a structural ceiling that no individual model can blow past.
- A clean responsibility split between provider (wire format), framework (catalog, execution, loop), and tool author (the work itself).

## Out of scope (for now)

- **Per-tool authorization.** Today the agent whitelists *MCP servers*; everything those servers expose is in. Per-tool grants are deferred until there's a real consumer.
- **Tool result caching, idempotency, retries.** Out of scope for v1. The model decides what to retry.
- **Tool timeouts.** Cancellation flows from the agent's `CancellationToken`; per-tool wall-clock budgets are deferred.
- **Tool-call streaming UI.** The bus carries tool-call fragments (`agent:tool-call:<id>`) as they arrive, but the chat UI today renders them as a single block on completion. Streaming the arguments live is an open UX question, not an architecture one.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                            Agent loop                              │
│  ┌────────────┐  prompt   ┌────────────────┐                       │
│  │  Context   │──────────▶│ ILanguageModel │                       │
│  │ + system   │           │   (provider)   │                       │
│  │ + ephemera │           └───────┬────────┘                       │
│  └────────────┘                   │ fragments (text + thought + tools)
│        ▲                          ▼                                │
│        │                 ┌────────────────┐                        │
│        │                 │ InferenceRunner│ accumulates fragments, │
│        │                 │                │ publishes events       │
│        │                 └───────┬────────┘                        │
│        │                         │ InferenceOutcome                │
│        │                         ▼                                 │
│        │              ┌──────────────────────┐                     │
│        │              │ Tool calls present?  │ no  ─▶ done         │
│        │              └──────────┬───────────┘                     │
│        │                         │ yes                             │
│        │                         ▼                                 │
│        │            ┌─────────────────────────┐                    │
│        │            │ IToolCallDispatcher     │ parallel fan-out;  │
│        │            │  → MCP HTTP client      │ source-prefix      │
│        │            └────────────┬────────────┘ routing            │
│        │                         │                                 │
│        │                         ▼                                 │
│        │             ┌─────────────────────┐                       │
│        └─────────────│ Tool turns persist  │ in original call order│
│                      └─────────────────────┘                       │
│                                                                    │
│            (loop again unless this was iteration N = TurnLimit)    │
└─────────────────────────────────────────────────────────────────────┘
```

The provider is a thin wire layer. The agent loop owns the catalog, decides when to dispatch, runs the dispatcher, and decides when to stop.

## Turn model

`ModelTurn` is a single record (no inheritance hierarchy) with optional fields that different roles use:

```csharp
public record ModelTurn(
    ModelRole Role,
    string Content,
    DateTimeOffset Timestamp,
    string? ChannelId = null) : IContextEntry
{
    public ImmutableArray<ToolCall> ToolCalls { get; init; } = [];   // assistant turns that emitted calls
    public ToolCall? ToolCall { get; init; }                         // the call this Tool turn responds to
    public bool IsError { get; init; }                               // true on a Tool turn whose dispatch failed
    public ImmutableArray<Attachment> Attachments { get; init; } = [];
}
```

`ModelRole` is the discriminator. The values that matter to tool calling:

- **`Assistant`** — the model's reply. May carry plain text in `Content`, may carry `ToolCalls`, may carry both.
- **`Tool`** — a tool *result*. `Content` is the dispatcher's flattened text, `ToolCall` is the originating call (so re-prompting can correlate), `IsError` flags failures so the model gets to see the failure mode.
- **`Thought`** — model reasoning where the provider exposes it (Ollama `think:` blocks, etc.). Persisted but not re-prompted.
- **`SystemEphemeral`** — the per-turn ephemeral block injected before the user turn. Distinct from the persistent `System` role so providers can render it without confusing it with the system prompt; never persisted. See [prompt-context.md](prompt-context.md).
- **`User` / `FrameworkUser`** — user input, with `FrameworkUser` reserved for turns the framework synthesized on the user's behalf (heartbeats will use this).

`ModelTurn` is the *single* turn type — there is no `TextTurn` / `ToolCallTurn` / `ToolResultTurn` hierarchy; an earlier draft of this design proposed that split, and it didn't survive the implementation.

## Tool catalog

Tools are descriptors + dispatch — never an interface that ties the two together.

```csharp
public sealed record ToolGroup(string Source, IReadOnlyList<ToolDescriptor> Tools);
public sealed record ToolDescriptor(string Name, string? Description, IReadOnlyList<ToolParameter> Parameters);
public sealed record ToolParameter(string Name, string? Description, string Type, bool Required);
```

The catalog is per-agent. `AgentManager` calls [`IModelContextProtocolToolDiscovery.DiscoverAsync`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/IModelContextProtocolToolDiscovery.cs) for each whitelisted MCP server (resolved through `IModelContextProtocolServerRegistry`), each returning a `ToolGroup` whose `Source` is the server's registered name. The grouped catalog is passed into the agent constructor as `ImmutableArray<ToolGroup>` and stays fixed for the life of the agent — a config reload rebuilds it.

`AgentConfig.ModelContextProtocolServers` (JSON `mcpServers`) is the whitelist. Omit it (or set to `null`) and the agent sees every server the host knows about, including the host's own listener (`llamashears`). See [mcp.md](mcp.md).

There is no separate "internal tool" path. Tools registered with `services.AddMcpServer().WithTools<T>()` (the bundled filesystem and memory tools) live behind the host's own MCP listener and are discovered through the same MCP protocol the agent would use to talk to any external server.

## Tool name encoding (source prefix)

Two facts to coexist:

- A model emits a single tool name string (`name: "file_read"`).
- The framework needs to know which server the call routes to.

The convention: tool names are reported to the model as `<source>__<name>` (double underscore separator). The provider decodes the prefix when it builds the `ToolCall`:

```csharp
public sealed record ToolCall(
    string Source,           // server name; "" if the model emitted no prefix
    string Name,             // bare tool name
    string ArgumentsJson,
    string? CallId = null);
```

`ModelContextProtocolToolCallDispatcher.DispatchAsync`:

- Empty `Source` → reject with an error result (`"Tool call '<name>' was rejected: the model emitted an unprefixed name"`). The model sees the failure on the next iteration and can try again.
- `Source` not in the registry → reject with an error result (`"Tool call '<source>__<name>' was rejected: server '<source>' is not registered"`). Same recovery path.
- Otherwise: open an MCP client to the server's URI, call the tool with the decoded JSON arguments, flatten the response's `TextContentBlock` entries into a single string, and return `ToolCallResult(Content, IsError)`.

The `__` separator is a soft convention — the dispatcher never *parses* the name, it relies on the provider's fragment-decode to have split it cleanly. If you add a provider, that's where to enforce the split.

## Streaming fragments

`ILanguageModel.PromptAsync(prompt, options)` returns `IAsyncEnumerable<IModelResponseFragment>`. The four kinds:

- **`IModelTextResponse`** — appended to the assistant turn's `Content`.
- **`IModelThoughtResponse`** — accumulated into the thought turn (rendered separately in the UI; see `agent:thought:<id>`).
- **`IModelToolCallFragment`** — a fully-decoded `ToolCall` (the provider has already split the source prefix and parsed the arguments JSON). Models that emit tool calls one fragment at a time still surface as one `IModelToolCallFragment` per call once the call is complete; per-character argument streaming is an internal concern of the provider.
- **`IModelCompletionResponse`** — final fragment carrying the token count.

[`InferenceRunner`](../../src/LlamaShears.Core/InferenceRunner.cs) consumes the stream, accumulates per-kind state, publishes a fragment event for each chunk (`agent:message`, `agent:thought`, `agent:tool-call`), and on completion publishes the final `agent:turn` events for the assistant turn (with text + tool calls) and any thought turn.

## Tool execution

Per-call rules:

1. **Parallel by default.** When a model emits N tool calls in one response, `Agent.DispatchToolCallsAsync` launches N tasks via `Task.WhenAll`. Tools are responsible for their own concurrency (locking, transactional integrity, idempotency).
2. **Cancellation propagates.** The agent's `CancellationToken` flows into every dispatch.
3. **Failure is structured, not thrown.** A failed dispatch returns a `ToolCallResult(Content, IsError: true)`; the loop converts it to a `Tool` turn with `IsError = true`. The model sees the error in its next prompt and decides what to do.
4. **No timeouts in v1.** Cancellation handles host shutdown; per-tool wall-clock budgets are deferred.
5. **Bearer auth flows for loopback calls.** The MCP HTTP client is registered with `LoopbackBearerHandler` as a `DelegatingHandler`; for any request whose URI matches the host's own listener, the handler reads `ICurrentAgentAccessor.Current` and mints a fresh per-call bearer token. External MCP servers see no `Authorization` header from the framework — they're trusted on connect.
6. **Tool turn order is deterministic.** Results are persisted in *original call order*, even though dispatch is parallel and `agent:tool-result:<id>` events fire in arrival order. Some providers pair tool calls and tool results positionally, not by id; deterministic order keeps re-prompting honest.

## Loop termination

The agent loop terminates when:

- The model produces an outcome with no tool calls (the assistant turn is the answer), **or**
- The current iteration is the `Tools.TurnLimit`-th iteration (the *final iteration*, run with no tools available — see below), **or**
- An exception propagates up that the loop doesn't catch (`CompactionFailedException`, `OperationCanceledException`).

There is no `ReportStatus` tool, despite older drafts of this design proposing one. The implemented loop reaches the same end state — *"the model gets a chance to wrap up in plain text before the loop exits"* — by running the final iteration without any tools. If the model still tries to emit tool calls on that turn, they're logged and dropped.

This is structurally cheaper than `ReportStatus`: no extra round-trip on the happy path, no schema for the model to learn, no special-case interception inside the dispatcher. The loss is that the model has no explicit "I have more to do, hand control back to the host" signal — the design vocabulary calls that "yielding to the host." Today, "yielding" is implicit: hit the turn limit, write text, the next user input picks the conversation back up. If a real need surfaces for explicit yielding (e.g. autonomous run-to-completion on heartbeats), `ReportStatus` is worth revisiting.

## Iteration limit

`AgentConfig.Tools.TurnLimit` (default `8`) caps how many model round-trips one batch can drive. The mechanic is:

- One iteration = one model prompt round, regardless of how many parallel tool calls fan out from it.
- Iterations 1 to `N-1` see the full tool catalog.
- Iteration `N` (the final one) sees an empty tool catalog and an `important_message` in the ephemeral block telling the model to wrap up in text. Any tool calls it still emits are dropped.

`N` is therefore the *total* iteration ceiling and `N-1` is the *tool-using* ceiling. This is intentional: the configured number is the number of model calls the agent will make, full stop. Setting `turnLimit: 1` is the degenerate case — one final-iteration call, no tools available.

The default of 8 is a "tall enough that healthy agents don't notice, low enough that a confused model burns out within a batch" guess. Tune per-agent in the config when you have real evidence one way or the other.

## Provider responsibility split

A provider implementation (`OllamaLanguageModel` today, future others) is responsible for:

- Translating `ModelPrompt` (a sequence of `ModelTurn`s) and the available tool catalog into the wire format the underlying API expects.
- Streaming the API response back as `IModelResponseFragment`s, including parsing tool calls into `ToolCall(Source, Name, ArgumentsJson, CallId?)` with the source prefix split.
- Mapping `ModelRole` (including `Tool` and `SystemEphemeral`) onto the API's role vocabulary.

A provider is explicitly **not** responsible for:

- Choosing which tools are available (catalog is built by `AgentManager` from the agent's MCP server whitelist).
- Executing any tool (that's the dispatcher).
- Deciding when the agent loop ends (that's `Tools.TurnLimit`).
- Handling tool errors (the framework wraps them into `Tool` turns).
- Knowing about `<source>__<name>` (the framework decides the convention; the provider just needs to round-trip whatever name the catalog handed it).

## Anti-goals

- The framework will not introspect tool implementations to infer behavior. Tools declare their schema; the framework does not parse implementations.
- The framework will not retry failed tool calls. Failures surface to the model.
- The framework will not serialize concurrent tool calls. Tools that need that serialize themselves.
- The framework will not synthesize provider-specific role vocabularies. Providers map between `ModelRole` and the API's roles.

## References

- [agent-loop.md](agent-loop.md) — the loop that drives this.
- [mcp.md](mcp.md) — the dispatch and authentication path.
- [memory.md](memory.md) — the bundled memory tools (`memory_store`, `memory_search`, `memory_index`).
- [`ToolCall`](../../src/LlamaShears.Core.Abstractions/Provider/ToolCall.cs)
- [`ModelTurn`](../../src/LlamaShears.Core.Abstractions/Provider/ModelTurn.cs)
- [`ModelRole`](../../src/LlamaShears.Core.Abstractions/Provider/ModelRole.cs)
- [`InferenceRunner`](../../src/LlamaShears.Core/InferenceRunner.cs)
- [`Agent.DispatchToolCallsAsync`](../../src/LlamaShears.Core/Agent.cs)
