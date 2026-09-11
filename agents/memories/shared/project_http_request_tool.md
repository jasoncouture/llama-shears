---
name: http_request is the native HTTP client
description: Bundled MCP http_request for GET/POST/headers/timeouts; saveAs writes the body into the workspace for binaries
type: project
---

`http_request` (`LlamaShears.Api.Tools.ModelContextProtocol.Http`) is the first-class HTTP client. Agents should use it instead of `shell_run` + curl + jq.

- Absolute `http`/`https` only. Methods: GET, HEAD, POST, PUT, PATCH, DELETE. GET/HEAD refuse a body.
- Headers are a string map. Refuse Host, Content-Length, Transfer-Encoding, Connection, and other hop-by-hop headers.
- Timeout 30s default, 1–120s. Per-call CTS (not a new HttpClient).
- HTTP 4xx/5xx complete the tool (`ok=false`, `status` set). Transport/timeout set `error` / `timedOut`.
- Inline text body capped at 64 KiB (`truncated`). Binary omitted (`binary=true`).
- `saveAs` writes the full body into the workspace — same write confinement as `file_write` (`WorkspacePathResolver`, `system/` banned, protection policy). 512 MiB cap (`savedTruncated`) so Go binaries and small archives fit. Existing file requires `overwrite=true`. This is how agents fetch images/PDFs/binaries.
- Authenticated agent required. Log method + host, never Authorization values.

This is not a security sandbox. Agents already have `shell_run`. MCP allow/deny of the tool is the gate.
