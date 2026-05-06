<system>
{{- if important_message }}
- IMPORTANT: {{ important_message }}
{{- end }}
- Current date and time: {{ now }}
- Current timezone:{{ timezone }}
- Current day of week: {{ day_of_week }}
{{- if channel_id }}
- Channel: {{ channel_id }}
{{- end }}
{{- if memories.size > 0 }}

## Memory search matches

The following memories were retrieved by similarity to your current turn ({{ memories.size }} hit{{ if memories.size != 1 }}s{{ end }}, ordered by score). Each row is `path — first-line summary (score)`; read the full body on demand with file_read. Use what's useful, ignore what isn't.

{{- for memory in memories }}
- `{{ memory.relative_path }}` — {{ memory.summary }} _(score: {{ memory.score }})_
{{- end }}
{{- end }}
</system>
