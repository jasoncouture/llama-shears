# LlamaShears.Core.Abstractions.Provider.ModelTokenInformationContextEntry

Assembly: `LlamaShears.Core.Abstractions`

Persisted entry recording the cumulative model token usage observed
at a point in the conversation. Read by
[IAgentContext](../Agent/Persistence/IAgentContext.md).`TokenCount`
to surface the latest value without re-counting.

## Parameters

- `TokenCount` — Cumulative token count reported by the model at the time the entry was appended.

## Properties

### `TokenCount`

Cumulative token count reported by the model at the time the entry was appended.

## Methods

### `ModelTokenInformationContextEntry`(int TokenCount)

Persisted entry recording the cumulative model token usage observed
at a point in the conversation. Read by
[IAgentContext](../Agent/Persistence/IAgentContext.md).`TokenCount`
to surface the latest value without re-counting.

#### Parameters

- `TokenCount` — Cumulative token count reported by the model at the time the entry was appended.

