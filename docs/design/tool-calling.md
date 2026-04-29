# Tool calling

Design doc for the LlamaShears tool-calling subsystem. Nothing in this document is implemented yet; it captures the decisions that came out of the design conversation and the open implementation choices that will be settled when the foundations land.

## Goals

- A single tool-calling abstraction that works equally well for in-process C# tools and tools exposed by remote MCP servers. MCP is a first-class citizen, not a follow-up.
- A turn model that can represent text, tool-call requests, and tool results without polymorphic-class inheritance.
- A streaming wire format that can carry tool-call deltas as the model produces them.
- A deterministic agent loop with explicit termination, structured "what next" handoff to the next frame, and an iteration ceiling that no individual model can blow past.
- A clean responsibility split between provider (wire format), framework (catalog, execution, loop), and tool author (the work itself).
- Behavior that lands callers in the pit of success per [ADR-0007](../adr/0007-pit-of-success.md): obvious uses are correct uses; misuse is structurally inconvenient.

## Out of scope (for now)

- Choice of MCP transport (`stdio`, HTTP, WebSocket) — to be picked when the MCP client lands.
- Choice of JSON schema library / generator (`System.Text.Json` schema, `JsonSchema.Net`, source generator) — to be picked when internal-tool schema generation lands.
- Default per-agent iteration ceilings and timeout values — to be set with real model traffic in hand.
- Tool authorization / per-agent tool grants — eventually a real concern; deferred.
- Tool result caching, idempotency, retries — out of scope for v1.

## Architecture overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                            Agent loop                              │
│  ┌────────────┐  prompt   ┌────────────────┐                       │
│  │  Context   │──────────▶│ ILanguageModel │                       │
│  │ (turns)    │           │   (provider)   │                       │
│  └────────────┘           └───────┬────────┘                       │
│        ▲                          │ fragments (text + tool calls)  │
│        │                          ▼                                │
│        │                 ┌────────────────┐                        │
│        │                 │ Fragment       │                        │
│        │                 │ accumulator    │                        │
│        │                 └───────┬────────┘                        │
│        │                         │                                 │
│        │                         ▼                                 │
│        │                 ┌────────────────┐                        │
│        │                 │ ReportStatus?  │ yes ─▶ exit loop       │
│        │                 └───────┬────────┘                        │
│        │                         │ no                              │
│        │                         ▼                                 │
│        │                 ┌────────────────┐                        │
│        │                 │  Tool runner   │ creates DI scope per   │
│        │                 │   (parallel)   │ call, dispatches via   │
│        │                 └───────┬────────┘ ToolCatalog (local +   │
│        │                         │           MCP), captures result │
│        │                         ▼                                 │
│        │                 ┌────────────────┐                        │
│        └─────────────────│  Tool result   │ appended to context    │
│                          │     turns      │                        │
│                          └────────────────┘                        │
└─────────────────────────────────────────────────────────────────────┘
```

The provider is a thin wire layer. The agent loop owns the tool catalog, decides when to run tools, runs them, and decides when to stop.

## Turn model

The current `ModelTurn` is a record `(Role, Content, Timestamp)`. Tool calling needs richer structure than a single `Content` string carries.

**Decision: polymorphic via interfaces, not class inheritance.**

`ModelTurn` becomes an interface. Concrete records implement it. Each variant carries only the fields that variant needs.

```csharp
public interface IModelTurn
{
    ModelRole Role { get; }
    DateTimeOffset Timestamp { get; }
}

public sealed record TextTurn(
    ModelRole Role,
    string Content,
    DateTimeOffset Timestamp) : IModelTurn;

public sealed record ToolCallTurn(
    ModelRole Role,             // Assistant or FrameworkAssistant
    string CallId,              // provider-supplied correlation id
    string ToolName,
    string ArgumentsJson,       // raw JSON; not yet validated
    DateTimeOffset Timestamp) : IModelTurn;

public sealed record ToolResultTurn(
    string CallId,              // matches the originating ToolCallTurn
    string ResultJson,          // structured success or serialized error
    bool IsError,
    DateTimeOffset Timestamp) : IModelTurn
{
    public ModelRole Role => ModelRole.Tool; // see below
}
```

Names are placeholders pending implementation; the shape is what matters.

A new `ModelRole.Tool` value lands at the same time as the polymorphic refactor — tool-result turns need a role that providers can map to the model's tool/function-result wire role (OpenAI `tool`, Anthropic `tool_result`, Ollama follows OpenAI shape).

`Content`-keyed APIs that exist today (e.g. anything that accesses `turn.Content`) get rewritten against the polymorphic shape. This is a breaking refactor across the provider layer; it lands in one commit so the solution is consistent.

## Tool catalog

Tools are exposed via a single interface:

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    JsonElement InputSchema { get; }   // JSON Schema describing arguments
}
```

Execution is a *separate* contract:

```csharp
public interface IToolHandler
{
    ValueTask<ToolResult> InvokeAsync(
        JsonElement arguments,
        CancellationToken cancellationToken);
}
```

A tool's schema (`ITool`) and its execution (`IToolHandler`) are decoupled so that:

- Schema can be authored once and reused across multiple invocation paths (local synchronous, local async, remote-via-MCP).
- The framework can list tools (catalog UI, debugging, status reports) without instantiating handlers.
- Internal tools written in C# generate their schema from a typed parameter record; MCP-exposed tools carry their schema directly from the MCP server.

A `ToolCatalog` aggregates `ITool` from two sources:

- **Local tools.** Registered in DI via an extension like `services.AddTool<TParameters, THandler>()` where `TParameters` is the typed parameter record; the handler accepts the record (deserialized from `JsonElement`) and returns a `ToolResult`. Schema is generated from `TParameters` at registration time.
- **MCP tools.** A connected MCP client lists tools on connect; each surfaces as an `ITool` whose schema is whatever the MCP server reported, and whose handler is an MCP-RPC wrapper.

The agent loop pulls from the catalog when constructing a `ModelPrompt`. The catalog can be filtered per agent if/when per-agent tool grants land.

## Schema authoring

- **Internal C# tools.** Schema is generated from a typed parameter record. The generator details are open (STJ schema, JsonSchema.Net, source generator); the contract is "give me a `Type`, get me a `JsonElement` describing it." Generated schemas are deterministic and versioned by parameter type.
- **MCP tools.** Use the schema the server reports verbatim. Do not re-derive or reformat it.

The `ITool.InputSchema` accessor exposes whichever path produced the schema; consumers don't need to know.

## Streaming fragments

`IModelResponseFragment` already carries text fragments. Tool calling adds:

```csharp
public interface IToolCallFragment : IModelResponseFragment
{
    string CallId { get; }
    string ToolName { get; }      // may arrive before arguments are complete
    string ArgumentsDelta { get; } // appended to the in-progress arguments string
    bool IsComplete { get; }       // true on the fragment that finalises the call
}
```

The fragment accumulator stitches deltas back into a `ToolCallTurn` once `IsComplete` arrives. Models that emit complete tool calls in one shot (no streaming) produce a single fragment with `IsComplete = true`.

## Tool execution

Per-call rules:

1. **Each tool invocation gets its own DI scope.** Created when the tool runner picks the call up, disposed when the invocation returns. Tools may take scoped dependencies (DbContext, etc.) without captive-dependency hazards.
2. **Cancellation is propagated.** The agent's cancellation token flows into every invocation. Tools are expected to honour it.
3. **Failure surfaces to the model.** A throwing tool produces a `ToolResultTurn` with `IsError = true` and the exception serialised into `ResultJson` (type, message, possibly a sanitised stack-tail). The model sees the error in its next prompt and decides what to do — retry with different arguments, call a different tool, give up. The loop does not abort on tool failures.
4. **Parallel execution is the default.** When a model emits N tool calls in one response, the runner dispatches them concurrently. Tools are responsible for their own concurrency (locking, transactional integrity, idempotency). A tool that cannot tolerate parallel invocation is a tool that needs to handle that internally; the framework does not provide serialization as a service.
5. **No timeouts in v1.** Cancellation handles the host-shutdown case; per-tool timeouts are deferred until there's a clear demand.

## Loop termination: `ReportStatus` as required terminator

The agent loop terminates when the model invokes a built-in `ReportStatus` tool. The tool is always in the catalog and is intercepted by the loop rather than dispatched through the normal tool runner.

```jsonschema
{
  "name": "ReportStatus",
  "description": "Hand control back to the host. Call when finished, or to escalate to the next frame.",
  "parameters": {
    "type": "object",
    "properties": {
      "done":         { "type": "boolean" },
      "instructions": { "type": "string"  }
    },
    "required": ["done", "instructions"]
  }
}
```

Semantics:

- **`done: true`** — the agent considers its current work complete. The loop ends; the agent is idle until the next frame tick.
- **`done: false`** — the agent has more to do but is yielding to the host (typically because work is naturally chunked). `instructions` becomes the content of a `FrameworkUser` turn injected at the start of the next frame.
- **The `ReportStatus` invocation itself does not enter the persistent context.** When the agent's context is serialised for the next prompt, calls to `ReportStatus` are filtered out. The model never sees its own status reports in subsequent turns.

If the model produces a text-only assistant turn without calling `ReportStatus`, the loop sends a single `FrameworkUser` reminder ("call ReportStatus when done; do not produce final text without it") and re-prompts. One round-trip in the misuse case; zero in the happy path.

This pattern collapses the "are you done?" question into the model's existing tool-calling muscles, which models execute far more reliably than they execute freeform JSON or terminal-marker conventions.

## Iteration limits

Recursive tool-call cycles can run away regardless of which model is driving — the model gets stuck in a small set of tool calls it keeps re-issuing, never reaching `ReportStatus`. Per-agent iteration limits are a structural safeguard, not a per-model workaround.

- Each agent has a configurable maximum loop iterations per frame (`MaxIterationsPerFrame`).
- One iteration = one model prompt round (regardless of how many parallel tool calls fan out from it).
- When the limit is hit before the model calls `ReportStatus`, the loop synthesises a status as if `ReportStatus(done: false, instructions: "Iteration limit reached. Consider whether your approach is converging; you may want to reduce scope or change tactics.")` had been called. The agent gets context on *why* it was cut off in the next frame.

The default value is intentionally not specified here — it should be set against real model traffic. The right floor is "enough that healthy agents never hit it" and the right ceiling is "low enough that a runaway burns out within a frame."

## Provider responsibility split

Provider implementations (Ollama, future OpenAI, future Anthropic, etc.) are responsible for:

- Serialising the tool catalog (`IReadOnlyList<ITool>`) into the underlying API's wire format.
- Translating `ModelTurn`s — including tool-call turns and tool-result turns — into the API's expected message shape.
- Parsing streaming responses into `IModelResponseFragment` instances, including tool-call fragments.
- Mapping `ModelRole` (including the new `Tool` value) onto the API's role vocabulary.

Provider implementations are explicitly **not** responsible for:

- Choosing which tools are available.
- Executing any tool.
- Deciding when the agent loop ends.
- Handling tool errors.
- Knowing what `ReportStatus` does.

`ReportStatus` is just another tool from the provider's perspective. The framework intercepts the call before dispatch.

## Open implementation questions

These will be resolved when the foundations land; recording here so the conversation doesn't have to re-derive them.

- **Schema generator.** Source generator vs. STJ-runtime vs. JsonSchema.Net? Decide based on AOT compatibility, ergonomics for parameter records, and dependency weight.
- **MCP client library.** Use the `Microsoft.Extensions.AI` MCP client, the `ModelContextProtocol` SDK, or roll a thin one? Hinges on transport support and lifecycle management.
- **`ToolResult` shape.** A `record ToolResult(JsonElement Body, bool IsError)` is the obvious default; whether to model success/error as a discriminated result or a flag is open.
- **Tool registration ergonomics.** `services.AddTool<TParams, THandler>()` is the proposed surface; whether handlers can be authored as static methods (lighter) or must be classes (DI-friendly) is open.
- **Persistent vs. transient context.** Where exactly the "filter out `ReportStatus` calls when serialising context" rule lives — in the prompt builder, in the context store, or as a serialisation strategy — is open.
- **Telemetry.** Tool-call counts, durations, and outcomes are obvious metrics; the wiring point (a logger? a `Meter`? structured events?) is open.

## Anti-goals

To make the boundaries explicit:

- The framework will not introspect tool implementations to infer behaviour. Tools declare what they accept (schema) and what they return; the framework does not parse implementations.
- The framework will not retry failed tool calls. Failures surface to the model and the model decides.
- The framework will not serialise concurrent tool calls. Tools that need that serialise themselves.
- The framework will not convert provider-specific role vocabularies into custom roles to "match" what the model produced. Providers map between `ModelRole` and the API's vocabulary; the rest of the system speaks `ModelRole`.

## References

- [ADR-0007: Pit of success](../adr/0007-pit-of-success.md) — the rationale lens.
- [`Provider.Abstractions/ModelRole.cs`](../../src/LlamaShears.Provider.Abstractions/ModelRole.cs) — current role enum, gains `Tool`.
- [`Provider.Abstractions/ModelTurn.cs`](../../src/LlamaShears.Provider.Abstractions/ModelTurn.cs) — current turn record, refactored to interface + variant records.
