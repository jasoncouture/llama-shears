# LlamaShears.Core.Abstractions.Agent.AgentModelConfig

Assembly: `LlamaShears.Core.Abstractions`

Per-agent language-model selection and per-call options.

## Parameters

- `Id` — Provider/model identifier of the language model.
- `Think` — How aggressively a thinking-capable model should reason; ThinkLevel.`None` disables thinking.
- `ContextLength` — Override for the model's context-window size; `null` uses provider default.
- `KeepAlive` — Provider-specific keep-alive for the model; `null` uses provider default.
- `TokenLimit` — Maximum tokens this agent is allowed to consume in a single response; `0` = unbounded.
- `Options` — Free-form provider/model JSON overrides merged on top of host defaults.

## Properties

### `ContextLength`

Override for the model's context-window size; `null` uses provider default.

### `Id`

Provider/model identifier of the language model.

### `KeepAlive`

Provider-specific keep-alive for the model; `null` uses provider default.

### `Options`

Free-form provider/model JSON overrides merged on top of host defaults.

### `Think`

How aggressively a thinking-capable model should reason; ThinkLevel.`None` disables thinking.

### `TokenLimit`

Maximum tokens this agent is allowed to consume in a single response; `0` = unbounded.

## Methods

### `AgentModelConfig`(ModelIdentity Id, ThinkLevel Think, Nullable<int> ContextLength, Nullable<TimeSpan> KeepAlive, int TokenLimit, Nullable<JsonElement> Options)

Per-agent language-model selection and per-call options.

#### Parameters

- `Id` — Provider/model identifier of the language model.
- `Think` — How aggressively a thinking-capable model should reason; ThinkLevel.`None` disables thinking.
- `ContextLength` — Override for the model's context-window size; `null` uses provider default.
- `KeepAlive` — Provider-specific keep-alive for the model; `null` uses provider default.
- `TokenLimit` — Maximum tokens this agent is allowed to consume in a single response; `0` = unbounded.
- `Options` — Free-form provider/model JSON overrides merged on top of host defaults.

