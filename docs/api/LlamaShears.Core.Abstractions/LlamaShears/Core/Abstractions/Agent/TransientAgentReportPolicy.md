# LlamaShears.Core.Abstractions.Agent.TransientAgentReportPolicy

Assembly: `LlamaShears.Core.Abstractions`

Controls whether a transient agent forwards its last assistant
text to the parent session when it goes idle. Absent from the
data scope means report-on (cron and heartbeat). Await-style
`subagent_run` sets [TransientAgentReportPolicy](TransientAgentReportPolicy.md).`ReportToParent` to
`false` so the caller collects the output once.

## Parameters

- `ReportToParent` — When `false`, skip the parent `ChannelMessage`.

## Fields

### `DataKey`

Data-scope key for the report policy.

## Properties

### `ReportToParent`

When `false`, skip the parent `ChannelMessage`.

## Methods

### `TransientAgentReportPolicy`(bool ReportToParent)

Controls whether a transient agent forwards its last assistant
text to the parent session when it goes idle. Absent from the
data scope means report-on (cron and heartbeat). Await-style
`subagent_run` sets [TransientAgentReportPolicy](TransientAgentReportPolicy.md).`ReportToParent` to
`false` so the caller collects the output once.

#### Parameters

- `ReportToParent` — When `false`, skip the parent `ChannelMessage`.

