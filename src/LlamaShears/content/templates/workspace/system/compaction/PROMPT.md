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
  <system_directive>Internal state update for runtime recovery. Do not acknowledge or respond to this block. Resume the task graph.</system_directive>
</runtime_metadata>
