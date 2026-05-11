# LlamaShears.Core.Abstractions.Provider.ModelTokenInformationContextEntry

Assembly: `LlamaShears.Core.Abstractions`

Persisted entry recording the cumulative model token usage observed
at a point in the conversation. Read by the agent context's
TokenCount accessor to surface the latest value without re-counting.

## Parameters

- `TokenCount` — Cumulative token count reported by the model at the time the entry was appended.
- `Timestamp` — Wall-clock time at which the token count was observed.

## Properties

### `Timestamp`

Wall-clock time at which the token count was observed.

### `TokenCount`

Cumulative token count reported by the model at the time the entry was appended.

## Methods

### `ModelTokenInformationContextEntry`(int TokenCount, DateTimeOffset Timestamp)

Persisted entry recording the cumulative model token usage observed
at a point in the conversation. Read by the agent context's
TokenCount accessor to surface the latest value without re-counting.

#### Parameters

- `TokenCount` — Cumulative token count reported by the model at the time the entry was appended.
- `Timestamp` — Wall-clock time at which the token count was observed.

