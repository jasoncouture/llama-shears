---
name: http_request is the native HTTP client
description: Bundled MCP http_request for GET/POST/headers/timeouts; saveAs writes the body into the workspace for binaries
type: project
---

`http_request` (`LlamaShears.Api.Tools.ModelContextProtocol.Http`) is the first-class HTTP client. Agents should use it instead of `shell_run` + curl + jq.

- Absolute `http`/`https` only. Methods: GET, HEAD, POST, PUT, PATCH, DELETE. GET/HEAD refuse a body. Content headers (Content-Type, etc.) require a body — do not attach an empty entity.
- Headers are a string map. Refuse Host, Content-Length, Transfer-Encoding, Connection, and other hop-by-hop headers.
- Timeout 30s default, 1–120s. Per-call CTS (not a new HttpClient). Named client: 10 redirects, `UseCookies = false` (handler is reused; do not replay Set-Cookie). Not an SSRF sandbox — agents have `shell_run`.
- HTTP 4xx/5xx complete the tool (`ok=false`, `status` set). Transport/timeout set `error` / `timedOut`. A failed `saveAs` sets `saveError` and leaves `ok` as the HTTP status. `error` means transport is dead — do not overload it with save failures.
- HEAD / 204 / 304 skip the body read. Inline text capped at 64 KiB; `truncated` only when more bytes remain. Hard cut drops an incomplete trailing UTF-8 sequence. Sniff NULs first, then trust image/audio/video/pdf/zip Content-Type. `application/octet-stream` is not forced binary.
- `saveAs` writes the full body into the workspace — same write confinement as `file_write` (`WorkspacePathResolver`, `system/` banned, protection policy). Stream to a sibling temp, then `File.Move` into place (failed/timed-out save must not clobber the destination). 512 MiB cap (`savedTruncated`) so Go binaries and small archives fit. Existing file requires `overwrite=true`. This is how agents fetch images/PDFs/binaries.
- Authenticated agent required. Log method + host, never Authorization values.

This is not a security sandbox. Agents already have `shell_run`. MCP allow/deny of the tool is the gate.
