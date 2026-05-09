<runtime_metadata>
[AMBIENT CONTEXT UPDATE]
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
- `{{ memory.relative_path }}` — {{ memory.summary }} _(score: {{ memory.score | math.round 2 }})_
{{- end }}
{{- end }}

## Instruction
This is a silent background update. Do not respond to this block. 
Use this information to inform your next thought or tool call, 
then continue your current task.
</runtime_metadata>
