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

[SYSTEM]
This is an internal state update for runtime recovery. Do not acknowledge or respond to this block. 
Resume the task graph.
</runtime_metadata>