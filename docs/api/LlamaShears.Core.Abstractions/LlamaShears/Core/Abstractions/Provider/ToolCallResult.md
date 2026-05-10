# LlamaShears.Core.Abstractions.Provider.ToolCallResult

Assembly: `LlamaShears.Core.Abstractions`

Output of dispatching a single [ToolCall](ToolCall.md) back to the
model.

## Parameters

- `Content` — String body fed back into the conversation.
- `IsError` — Whether the tool reported a failure; the agent loop uses this to decide whether to surface the error to the model.

## Properties

### `Content`

String body fed back into the conversation.

### `IsError`

Whether the tool reported a failure; the agent loop uses this to decide whether to surface the error to the model.

## Methods

### `ToolCallResult`(string Content, bool IsError)

Output of dispatching a single [ToolCall](ToolCall.md) back to the
model.

#### Parameters

- `Content` — String body fed back into the conversation.
- `IsError` — Whether the tool reported a failure; the agent loop uses this to decide whether to surface the error to the model.

