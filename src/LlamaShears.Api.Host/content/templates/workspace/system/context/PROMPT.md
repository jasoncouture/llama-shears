<system>
- Current date and time: {{ now }}
- Current timezone:{{ timezone }}
- Current day of week: {{ day_of_week }}
{{- if channel_id }}
- Channel: {{ channel_id }}
{{- end }}
{{- if memories.size > 0 }}
- Memory search matches
{{- for memory in memories }}
  - {{ memory.text }}
{{- end }}
{{- end }}
</system>
