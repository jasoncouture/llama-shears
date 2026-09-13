<runtime_metadata>
  <kind>compaction</kind>
{{- if important_message }}
  <important>{{ important_message }}</important>
{{- end }}
  <current_datetime>{{ now | format_datetimeoffset 'yyyy-MM-ddTHH:mm:sszzz' }}</current_datetime>
  <timezone>{{ timezone }}</timezone>
  <day_of_week>{{ day_of_week }}</day_of_week>
{{- if agent_state.channel_id }}
  <channel>{{ agent_state.channel_id }}</channel>
{{- end }}
  <system_directive>The last user message is a transcript of older conversation turns, not a live session. Do not continue that conversation in-character.</system_directive>
</runtime_metadata>
