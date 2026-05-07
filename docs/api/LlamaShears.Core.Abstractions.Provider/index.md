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

---

## LlamaShears.Core.Abstractions.Provider

- [AgentProviderOptions](LlamaShears/Core/Abstractions/Provider/AgentProviderOptions.md)
- [IContextEntry](LlamaShears/Core/Abstractions/Provider/IContextEntry.md)
- [IEmbeddingModel](LlamaShears/Core/Abstractions/Provider/IEmbeddingModel.md)
- [IEmbeddingProviderFactory](LlamaShears/Core/Abstractions/Provider/IEmbeddingProviderFactory.md)
- [IInferenceRunner](LlamaShears/Core/Abstractions/Provider/IInferenceRunner.md)
- [ILanguageModel](LlamaShears/Core/Abstractions/Provider/ILanguageModel.md)
- [IModelCompletionResponse](LlamaShears/Core/Abstractions/Provider/IModelCompletionResponse.md)
- [IModelResponseFragment](LlamaShears/Core/Abstractions/Provider/IModelResponseFragment.md)
- [IModelTextResponse](LlamaShears/Core/Abstractions/Provider/IModelTextResponse.md)
- [IModelThoughtResponse](LlamaShears/Core/Abstractions/Provider/IModelThoughtResponse.md)
- [IModelToolCallFragment](LlamaShears/Core/Abstractions/Provider/IModelToolCallFragment.md)
- [IProviderFactory](LlamaShears/Core/Abstractions/Provider/IProviderFactory.md)
- [InferenceOutcome](LlamaShears/Core/Abstractions/Provider/InferenceOutcome.md)
- [LanguageModelExtensions](LlamaShears/Core/Abstractions/Provider/LanguageModelExtensions.md)
- [ModelConfiguration](LlamaShears/Core/Abstractions/Provider/ModelConfiguration.md)
- [ModelIdentity](LlamaShears/Core/Abstractions/Provider/ModelIdentity.md)
- [ModelIdentityJsonConverter](LlamaShears/Core/Abstractions/Provider/ModelIdentityJsonConverter.md)
- [ModelIdentityTypeConverter](LlamaShears/Core/Abstractions/Provider/ModelIdentityTypeConverter.md)
- [ModelInfo](LlamaShears/Core/Abstractions/Provider/ModelInfo.md)
- [ModelPrompt](LlamaShears/Core/Abstractions/Provider/ModelPrompt.md)
- [ModelRole](LlamaShears/Core/Abstractions/Provider/ModelRole.md)
- [ModelTokenInformationContextEntry](LlamaShears/Core/Abstractions/Provider/ModelTokenInformationContextEntry.md)
- [ModelTurn](LlamaShears/Core/Abstractions/Provider/ModelTurn.md)
- [PromptOptions](LlamaShears/Core/Abstractions/Provider/PromptOptions.md)
- [SupportedInputType](LlamaShears/Core/Abstractions/Provider/SupportedInputType.md)
- [ThinkLevel](LlamaShears/Core/Abstractions/Provider/ThinkLevel.md)
- [ToolCall](LlamaShears/Core/Abstractions/Provider/ToolCall.md)
- [ToolCallResult](LlamaShears/Core/Abstractions/Provider/ToolCallResult.md)
- [ToolDescriptor](LlamaShears/Core/Abstractions/Provider/ToolDescriptor.md)
- [ToolGroup](LlamaShears/Core/Abstractions/Provider/ToolGroup.md)
- [ToolParameter](LlamaShears/Core/Abstractions/Provider/ToolParameter.md)

