# LlamaShears.Core.Abstractions.Agent.Sessions.EphemeralSessionRequest

Assembly: `LlamaShears.Core.Abstractions`

Caller-supplied configuration for an ephemeral session: which system
prompt template to render, any extra data the template needs, an
optional iteration cap, and an optional channel id for tagging events
and replies.

## Parameters

- `SystemPromptTemplate` — Template file name to feed into `PromptOptions.SystemPromptTemplate`
for every inference inside this session (e.g. `SUBAGENT.md`). Required.
- `TemplateData` — Optional key/value pairs merged into the session's data scope so the
template can interpolate them. `null` = no extra data.
- `MaxIterations` — Maximum number of inference iterations the session loop will run
before forcing exit. `null` uses the implementation
default.
- `ChannelId` — Caller-chosen channel id used for tagging events emitted from inside
the session and for the `:<channelId>` segment on the
`session_reply` event. `null` = the
implementation falls back to a per-session synthesized id.

## Properties

### `ChannelId`

Caller-chosen channel id used for tagging events emitted from inside
the session and for the `:<channelId>` segment on the
`session_reply` event. `null` = the
implementation falls back to a per-session synthesized id.

### `MaxIterations`

Maximum number of inference iterations the session loop will run
before forcing exit. `null` uses the implementation
default.

### `SystemPromptTemplate`

Template file name to feed into `PromptOptions.SystemPromptTemplate`
for every inference inside this session (e.g. `SUBAGENT.md`). Required.

### `TemplateData`

Optional key/value pairs merged into the session's data scope so the
template can interpolate them. `null` = no extra data.

## Methods

### `EphemeralSessionRequest`(string SystemPromptTemplate, IReadOnlyDictionary<string, object> TemplateData, Nullable<int> MaxIterations, string ChannelId)

Caller-supplied configuration for an ephemeral session: which system
prompt template to render, any extra data the template needs, an
optional iteration cap, and an optional channel id for tagging events
and replies.

#### Parameters

- `SystemPromptTemplate` — Template file name to feed into `PromptOptions.SystemPromptTemplate`
for every inference inside this session (e.g. `SUBAGENT.md`). Required.
- `TemplateData` — Optional key/value pairs merged into the session's data scope so the
template can interpolate them. `null` = no extra data.
- `MaxIterations` — Maximum number of inference iterations the session loop will run
before forcing exit. `null` uses the implementation
default.
- `ChannelId` — Caller-chosen channel id used for tagging events emitted from inside
the session and for the `:<channelId>` segment on the
`session_reply` event. `null` = the
implementation falls back to a per-session synthesized id.

