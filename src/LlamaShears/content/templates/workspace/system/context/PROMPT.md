<runtime_metadata>
  <kind>ambient_context_update</kind>
{{- if important_message }}
  <important>{{ important_message }}</important>
{{- end }}
  <current_datetime>{{ now | format_datetimeoffset 'yyyy-MM-ddTHH:mm:sszzz' }}</current_datetime>
  <timezone>{{ timezone }}</timezone>
  <day_of_week>{{ day_of_week }}</day_of_week>
{{- if agent_state.channel_id }}
  <channel>{{ agent_state.channel_id }}</channel>
{{- end }}
{{- if memories.size > 0 }}
  <memory_matches count="{{ memories.size }}">
    <description>Memories retrieved by similarity to the current turn, ordered by score. Full text available via file_read if strictly necessary.</description>
{{- for memory in memories }}
    <memory path="{{ memory.relative_path }}" score="{{ memory.score | math.round 2 }}">{{ memory.summary }}</memory>
{{- end }}
  </memory_matches>
{{- end }}
</runtime_metadata>
