# LlamaShears.Core.Abstractions.Provider

LLM-provider contracts for [LlamaShears](https://github.com/jasoncouture/llama-shears) — the largest abstraction in the public surface. Anything that talks to a chat model, runs an embedder, or surfaces a tool to the model goes through here.

## Public surface

### Provider plumbing

- **`IProviderFactory`** / **`IEmbeddingProviderFactory`** — host-side registries; `Name` selects between providers (e.g. `OLLAMA`, `ONNX`).
- **`ILanguageModel`** / **`IEmbeddingModel`** — per-model handles.
- **`IInferenceRunner`** — the inference loop primitive that the agent loop drives.
- **`AgentProviderOptions`** — per-agent options that layer over host defaults.

### Model identity / configuration

- **`ModelIdentity`** — `provider/model` pair, parsed and serialized via `ModelIdentityJsonConverter` / `ModelIdentityTypeConverter`.
- **`ModelConfiguration`** — the static config passed to `IProviderFactory.CreateModel`.
- **`ModelInfo`** — provider-supplied metadata (display name, supported inputs, max context window).
- **`SupportedInputType`** — feature-flag bitmask (text, images, …).
- **`ThinkLevel`** — opt-in reasoning effort knob.

### Prompt + response

- **`ModelPrompt`** / **`ModelTurn`** / **`ModelRole`** — the conversation shape.
- **`PromptOptions`** — per-call overrides (token limit, tool catalog).
- **`IModelResponseFragment`** + variants (`IModelTextResponse`, `IModelThoughtResponse`, `IModelToolCallFragment`, `IModelCompletionResponse`) — streamed pieces of a turn.
- **`ModelTokenInformationContextEntry`** — token-count context entries fed back into the conversation log.
- **`InferenceOutcome`** — terminal turn state.

### Tools

- **`ToolCall`** / **`ToolCallResult`** — the wire shapes for a single tool invocation.
- **`ToolDescriptor`** / **`ToolGroup`** / **`ToolParameter`** — the schema the model sees when it picks a tool.

### Extension points

- **`LanguageModelExtensions`** — helpers over `ILanguageModel`.
- **`IContextEntry`** — base interface for the polymorphic conversation log entries.

## See also

- [Architecture overview](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/architecture.md)
- [Tool calling](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/tool-calling.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.
