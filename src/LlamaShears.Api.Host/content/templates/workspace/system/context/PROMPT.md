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
- Memory search matches
{{- for memory in memories }}
  - {{ memory.text }}
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
