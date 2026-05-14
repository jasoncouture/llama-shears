<runtime_metadata>
[AMBIENT CONTEXT UPDATE]
{{- if important_message }}
- IMPORTANT: {{ important_message }}
{{- end }}
- Current date and time: {{ now | format_datetimeoffset 'yyyy-MM-ddTHH:mm:sszzz' }}
- Current timezone:{{ timezone }}
- Current day of week: {{ day_of_week }}
{{- if channel_id }}
- Channel: {{ channel_id }}
{{- end }}
{{- if todo.size > 0 }}

## TODO list

{{ todo.size }} item{{ if todo.size != 1 }}s{{ end }} on the agent's persistent list (1-based; addressable by index via the `todo_*` tools):

{{- for item in todo }}
- {{ item }}
{{- end }}
{{- end }}
{{- if memories.size > 0 }}

## Memory search matches

The following memories were retrieved by similarity to your current turn ({{ memories.size }} hit{{ if memories.size != 1 }}s{{ end }}, ordered by score). Each row is `path — first-line summary (score)`; Full text available via file_read if strictly necessary.

{{- for memory in memories }}
- `{{ memory.relative_path }}` — {{ memory.summary }} _(score: {{ memory.score | math.round 2 }})_
{{- end }}
{{- end }}
</runtime_metadata>