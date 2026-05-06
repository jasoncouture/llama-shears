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

The following memories were retrieved by similarity to your current turn ({{ memories.size }} hit{{ if memories.size != 1 }}s{{ end }}, ordered by score). Use what's useful, ignore what isn't.

{{- for memory in memories }}

### {{ memory.relative_path }} (score: {{ memory.score }})

{{ memory.content }}
{{- end }}
{{- end }}
{{- for file in files }}

## {{ file.name }}

{{ file.content }}
{{- end }}
{{- if additional_files.size > 0 }}

## Additional workspace files

{{- for name in additional_files }}
- {{ name }}
{{- end }}
{{- end }}
</system>
