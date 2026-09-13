# MCP server and authentication

LlamaShears speaks Model Context Protocol on both ends:

- **Inbound:** the host runs its own MCP server at `/mcp` over HTTP, gated by a custom bearer auth scheme. The bundled tools (filesystem + memory) live behind that listener.
- **Outbound:** when an agent's model emits a tool call, the framework dispatches it to the registered MCP server whose name matches the tool's source prefix. Calls that target the host's own listener carry an agent-scoped bearer minted on the fly.

Everything lives under [`Api/Authentication/`](../../src/LlamaShears.Api/Authentication/), [`Api/Tools/ModelContextProtocol/`](../../src/LlamaShears.Api/Tools/ModelContextProtocol/), and [`Core/Tools/ModelContextProtocol/`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/).

## Inbound: the host's MCP listener

`WebApplicationExtensions.UseApi` mounts the MCP server with `app.MapMcp("/mcp")`. The MCP machinery is `ModelContextProtocol.AspNetCore`; the host configures it in `ModelContextProtocolServiceCollectionExtensions.AddModelContextProtocol`:

```csharp
        services.AddMcpServer()
            .WithHttpTransport()
            .WithTools<FileTools>()
            .WithTools<MemoryTools>()
            .WithTools<CronTools>()
            .WithTools<TodoTools>()
            .WithTools<ShellTools>()
            .WithTools<SkillTools>()
            .WithTools<SessionTools>()
            .WithTools<SubagentTools>()
            .WithTools<CompactionTools>()
            .WithTools<DiscordTools>()
            .WithTools<HttpTools>()
            .WithTools<SqliteTools>();
```

Tool names follow a `<category>_<action>` convention so they group naturally in the model's tool listing:

| Tool | Purpose |
|------|---------|
| `file_read`, `file_list`, `file_write`, `file_append`, `file_delete`, `file_regex_replace`, `file_grep` | Filesystem operations. Relative paths resolve against the agent's workspace; absolute paths are honored. See *Filesystem tools* below. |
| `memory_store`, `memory_search`, `memory_index` | Memory operations. See [memory.md](memory.md). |
| `discord_list`, `discord_send` | Post to operator-configured Discord webhooks. See *Discord webhooks* below. |
| `http_request` | First-class HTTP client (GET/POST/headers/timeouts). See *HTTP requests* below. |
| `sqlite_query` | Execute SQL against a workspace-confined SQLite file. See *SQLite* below. |
| `session_list`, `session_send` | List the calling agent's live sessions; deliver a user-role turn to one of them. |
| `subagent_run` | Spawn a one-shot child of the calling root session. See *Sub-agents* below. |
| `context_compact` | Summarize older turns now (last six eligible kept). Same child path as automatic compaction; does not go through `/compact`. |

The internal listener is published into the host's outbound MCP registry under the fixed name `llamashears` (see [`ModelContextProtocolServerRegistry.BuildAllKnown`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/ModelContextProtocolServerRegistry.cs)). An agent that whitelists `"llamashears"` (or omits the whitelist) sees the bundled tools as `llamashears__file_read`, `llamashears__memory_store`, etc.

### Discord webhooks

[`DiscordTools`](../../src/LlamaShears.Api/Tools/ModelContextProtocol/Discord/DiscordTools.cs) posts to Discord Incoming Webhooks. The model never supplies a URL.

- **Config.** Bind `Discord:Webhooks` (a name → URL map) from `appsettings` or env vars such as `Discord__Webhooks__alerts=https://discord.com/api/webhooks/...`. Names are what `discord_list` returns and what `discord_send` accepts. URLs stay on the host.
- **Validation.** Startup (`DiscordWebhookOptionsValidator`) and send-time both require `https://discord.com` or `discordapp.com` `/api/webhooks/{id}/{token}` (optional `/api/vN`). Query strings, fragments, and other hosts are refused so a mis-typed config cannot become SSRF.
- **Send.** `discord_send` POSTs `{ content, optional username, allowed_mentions.parse = [] }` with `wait=true`. Content max 2000; username max 80. `@everyone` / `@here` / role / user parses are disabled. HTTP failures return status (and Retry-After on 429) without echoing the URL.
- **Auth.** Same as other bundled tools: an authenticated agent on the request. No webhooks configured, unknown name, or multiple webhooks without a name is a loud refuse.

Webhook URLs are secrets. Put them in `.local.env`, not in agent JSON.

### HTTP requests

[`HttpTools`](../../src/LlamaShears.Api/Tools/ModelContextProtocol/Http/HttpTools.cs) is the bundled `http_request` tool. Use it instead of `shell_run` + curl.

- **Request.** Absolute `http`/`https` only. Methods: GET, HEAD, POST, PUT, PATCH, DELETE. Optional header map (Host / Content-Length / hop-by-hop headers are refused). Content headers such as Content-Type require a body — they are refused otherwise so GET/HEAD never grow a `Content-Length: 0` entity. Optional raw body — forbidden on GET/HEAD. Missing Content-Type defaults to `application/json` when the body looks like JSON, otherwise `text/plain`. Timeout defaults to 30s (1–120). The named client follows up to 10 redirects and has cookies off so a reused handler cannot replay `Set-Cookie`.
- **Response.** Returns `status`, `reasonPhrase`, `ok`, flattened headers, `contentType`, and an inline `body`. HTTP error statuses complete the call (`ok=false`); they are not tool failures. HEAD / 204 / 304 skip the body. Inline text is capped at 64 KiB; `truncated=true` only when more bytes remain (an exact 64 KiB body is not truncated). A hard cut drops an incomplete trailing UTF-8 sequence. Binary is sniffed first (NUL in the first 512 bytes), then the Content-Type for image/audio/video/pdf/zip. `application/octet-stream` is not forced binary.
- **Save.** Optional `saveAs` writes the full body (text or binary) into the workspace, same confinement as `file_write` (no `system/`, no path escape, file-protection policy). The body is streamed to a sibling temp file and `File.Move`d into place so a failed or timed-out download does not clobber an existing file. Cap 512 MiB (`savedTruncated=true` if cut) so Go binaries and small archives fit. Existing files require `overwrite=true`. A failed save sets `saveError` and leaves `ok` as the HTTP status; `error` stays transport-only. Prefer `saveAs` for images, PDFs, binaries, and anything larger than the inline cap.
- **Auth.** Authenticated agent required. Transport/timeout failures set `error` / `timedOut`.

### SQLite

[`SqliteTools`](../../src/LlamaShears.Api/Tools/ModelContextProtocol/Sqlite/SqliteTools.cs) is the bundled `sqlite_query` tool. Use it instead of `shell_run` + `sqlite3`.

- **Path.** Workspace-relative (absolute paths must still resolve inside the workspace). Same write confinement as `file_write`: no `system/`, no escape, file-protection policy. Missing parent directories and the database file are created on open.
- **SQL.** Raw statements — DDL, DML, `SELECT`, `PRAGMA`. Multiple semicolon-delimited statements run in order. When several produce result sets, the last one is returned and `rowsAffected` is aggregated across the batch.
- **Budget.** `maxRows` defaults to 100 and is hard-capped at 1000. The reader stops after the cap (`truncated=true`); rows are not buffered unbounded.
- **Types.** INTEGER → JSON number, REAL → number, TEXT → string, NULL → `null`, BLOB → Base64 string.
- **Concurrency.** Connections open `ReadWriteCreate` + shared cache, then `PRAGMA journal_mode = WAL` and `PRAGMA busy_timeout = 5000` so concurrent sub-agents wait on locks instead of failing `SQLITE_BUSY`.
- **Errors.** SQL and IO failures populate `error` and return empty `columns` / `rows`. They do not throw out of the tool call.

This is not a security sandbox. Agents already have `shell_run`. MCP allow/deny of the tool is the gate.

### Sub-agents

[`SubagentTools`](../../src/LlamaShears.Api/Tools/ModelContextProtocol/Subagent/SubagentTools.cs) is the bundled `subagent_run` tool. It wraps [`ISubagentRunner`](../../src/public/LlamaShears.Core.Abstractions/Agent/ISubagentRunner.cs). The MCP HTTP request already joins the caller's [`IDataContextScope`](../../src/LlamaShears.Api/Authentication/RejectInvalidAgentBearerMiddleware.cs), so the scoped runner sees the parent session — same as `session_send`.

- **Spawn.** Root sessions only. Nested children are refused (same ban as [`PromptedAgentSpawner`](../../src/LlamaShears.Core/PromptedAgentSpawner.cs)). The child is a transient (`subagent-{guid}`) that renders bundled [`SUBAGENT.md`](../../src/LlamaShears/content/templates/workspace/system/SUBAGENT.md) unless the runner request overlays other template names. It shares the parent's workspace, memory, todos, and MCP allowlist — not a sandbox.
- **Await.** Default. The tool waits for child idle (timeout 120s, 1–600) and returns the last assistant text in `output`. Await sets `TransientAgentReportPolicy.ReportToParent = false` so the answer is not also enqueued as a parent `ChannelMessage`. Timeout publishes `AgentStop` for the child and returns `timedOut` plus any partial text.
- **Fire-and-forget.** `awaitResult=false` returns immediately after spawn (`ok=true`, `started=true`, `awaited=false`). The child reports later via `session_send` or the existing parent ChannelMessage path.
- **Overlays.** Optional `model` is a `provider/model` identity (`CompositeIdentity.TryParse`); unparseable values are refused. Optional `maxTurns` (1–64) sets the child's `Tools.TurnLimit`. Optional `context` is prepended to `prompt` as one user turn.
- **Result.** `ok` means the requested mode succeeded (awaited idle, or fire-and-forget spawn). `awaited` is whether you waited for the child to finish. `error` is refuse / spawn / timeout — not the child's HTTP-style status. Do not treat `ok=false` as the only way to notice a still-running child.

Locks are keyed on session canonical id, so the parent can hold `IAgentLock` while the child runs. See [agent-loop.md](agent-loop.md).

### Filesystem tools

Every filesystem tool resolves through [`IAgentWorkspaceLocator`](../../src/LlamaShears.Api/Tools/ModelContextProtocol/Filesystem/IAgentWorkspaceLocator.cs), which reads the bearer's agent id off `HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)`. From the agent id, it looks up the workspace path via `IAgentConfigProvider`. Relative paths in tool arguments are then `Path.Combine`d against that root; absolute paths are honored as-is.

The current contract is **no sandboxing**. An agent that sends an absolute path can read or write anywhere the host process can reach. This is the security-boundary remark from the project memory: tools enforce access policy in the MCP handler. If you want to lock filesystem tools to the workspace, that's a change to the path resolver in this directory.

`file_read` caps responses at 64 KiB by default (256 KiB hard ceiling). `file_grep` and `file_list` run within whatever directory the call names. `file_regex_replace` is single-file; `file_write` replaces, `file_append` adds.

### Bearer authentication

The MCP listener is fronted by an ASP.NET Core authentication scheme registered in [`AgentBearerServiceCollectionExtensions`](../../src/LlamaShears.Api/Authentication/AgentBearerServiceCollectionExtensions.cs):

- Scheme name: `AgentBearer` (constant `AgentBearerDefaults.Scheme`).
- Handler: [`AgentBearerHandler`](../../src/LlamaShears.Api/Authentication/AgentBearerHandler.cs) — pulls `Authorization: Bearer <token>` off the request, looks the token up in `IAgentTokenStore`, and projects an `AgentInfo` into a `ClaimsPrincipal` via `IAgentClaimsProjector`. The default projector ([`DefaultAgentClaimsProjector`](../../src/LlamaShears.Api/Authentication/DefaultAgentClaimsProjector.cs)) puts the agent id on `ClaimTypes.NameIdentifier`, with the model id and context window size as additional claims.
- Reject middleware: [`RejectInvalidAgentBearerMiddleware`](../../src/LlamaShears.Api/Authentication/RejectInvalidAgentBearerMiddleware.cs) returns 401 for any request to `/mcp` that didn't authenticate.

The default token store is [`InMemoryAgentTokenStore`](../../src/LlamaShears.Core/InMemoryAgentTokenStore.cs):

- Tokens are 32 random bytes, base64-encoded.
- Expiry is `AgentTokenStoreOptions.TokenLifetime` (configurable, default in the options class).
- Validation is **single-use** — `TryGetAgentInformation` calls `TryRemove` on the dictionary, so a token can be redeemed exactly once. A second call with the same token authenticates as if the token were unknown.
- `AgentTokenStoreSweeper` is a `BackgroundService` that periodically purges expired entries (so a single-use token that's never redeemed doesn't leak forever).

The single-use property is what makes the loopback dispatch story safe (next section): a leaked token is only good for one request, and the request must already be in flight.

## Outbound: dispatching tool calls

When the agent loop has tool calls to dispatch (see [agent-loop.md](agent-loop.md), step 8), it calls [`IToolCallDispatcher.DispatchAsync(ToolCall, cancellationToken)`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/IToolCallDispatcher.cs). The implementation is [`ModelContextProtocolToolCallDispatcher`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/ModelContextProtocolToolCallDispatcher.cs); it delegates the network call to [`IModelContextProtocolClient`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/IModelContextProtocolClient.cs), a transient typed `HttpClient` whose pipeline rewrites a sentinel URI into the real endpoint and stamps configured headers.

```
ToolCall(Source = "github", Name = "list_issues", ArgumentsJson = "{...}")
   │
   ▼
IModelContextProtocolClient.CallToolAsync("github", "list_issues", args)
   │  builds HttpClientTransport(endpoint = http://github/, sharedHttpClient)
   │  per-call McpClient — no pool
   ▼
HttpClient pipeline:
   1. ModelContextProtocolRoutingHandler — looks up "github" in the registry,
      rewrites RequestUri via IUriMerger (registry path appended, registry
      query wins on collisions), stamps configured headers (overwriting any
      caller-supplied values). Unknown server → synthesizes 404 locally with
      a JSON error body and never calls inner.
   2. LoopbackBearerHandler — if the (now real) URI is the internal listener,
      mints a one-shot agent bearer; otherwise passes through.
   3. Primary handler — IHttpClientFactory-pooled SocketsHttpHandler.
   ▼
client.CallToolAsync(name, args) → ToolCallResult(FlattenedText, IsError)
```

Per-call rules:

- **Empty `Source`** → dispatcher rejects with an error `ToolCallResult` before touching the client.
- **Unknown `Source`** → routing handler synthesizes a `404` locally; the SDK throws on the bad response; the dispatcher catches and returns an error result.
- **Server present but unreachable / throws** → catch, log, return error result. The agent loop persists the failure as a `Tool` turn with `IsError = true`.
- **No MCP-client pool.** Each `CallToolAsync` / `ListToolsAsync` constructs an `McpClient` over the shared `HttpClient`, awaits the operation, and disposes — handshake is paid per call. The routing handler in front of the pipeline means there's no per-server `HttpClient` to keep warm; the underlying `SocketsHttpHandler` is factory-pooled.

### Server registry

[`ModelContextProtocolServerRegistry`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/ModelContextProtocolServerRegistry.cs) merges two sources:

- Operator-configured servers from `ModelContextProtocolOptions.Servers` (`appsettings.json`). Each entry is a [`ModelContextProtocolServerOptions`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/ModelContextProtocolServerOptions.cs) — a `Uri` plus an optional `Headers` map that the routing handler stamps onto every outbound request to that server:

  ```json
  "ModelContextProtocol": {
    "Servers": {
      "github": {
        "Uri": "https://mcp.example.com/github",
        "Headers": {
          "Authorization": "Bearer ghp_abc123"
        }
      },
      "internal-tools": {
        "Uri": "http://10.0.1.5:8080/mcp"
      }
    }
  }
  ```

- The host's own listener, published under the fixed name `llamashears` if `IInternalModelContextProtocolServer.Uri` resolves to a non-null value. The internal URI is read off `IServer.Features` at request time, so it picks up whatever Kestrel actually bound to. The internal entry has no configured headers; auth is minted on the fly by `LoopbackBearerHandler`.

`Resolve(whitelist)` returns a name→`ModelContextProtocolServerOptions` map:

- The full registry if `whitelist` is `null` (omit `mcpServers` in agent config to opt into everything the host knows about).
- Only the named servers if `whitelist` is non-null. Names that don't resolve are logged at `Warning` and skipped — the agent gets a strictly smaller catalog rather than a hard failure.

`TryGet(name)` is the single-lookup path used by `ModelContextProtocolRoutingHandler`; it returns `null` for unknown names so the handler can synthesize a 404 instead of throwing.

### Routing handler

[`ModelContextProtocolRoutingHandler`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/ModelContextProtocolRoutingHandler.cs) is the outermost handler on the typed client's pipeline. The `IModelContextProtocolClient` impl always issues requests to a sentinel URI of the shape `http://{server-name}/...`. The routing handler:

1. Reads `request.RequestUri.Host` as the server name.
2. Calls `_registry.TryGet(serverName)`. On miss → returns a synthetic `404` response with body `{"error":"unknown MCP server '<name>'"}` (Content-Type `application/json`) and never calls `base.SendAsync`.
3. On hit, replaces `request.RequestUri` with `IUriMerger.Merge(config.Uri, sentinelUri)` — scheme/host/port and base path come from the registry entry; any tail path the MCP SDK transport added is appended; registry query keys win over caller-supplied ones.
4. Stamps every configured header onto `request.Headers`, removing any existing value first so the registry's configuration wins over anything the caller (or earlier handler) put there.
5. Calls `base.SendAsync`.

Order on the pipeline matters: `LoopbackBearerHandler` runs *after* routing, so when it inspects `request.RequestUri` to decide loopback, it's looking at the real endpoint (and matches the internal listener's host:port for the `llamashears` entry).

### Loopback bearer minting

[`LoopbackBearerHandler`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/LoopbackBearerHandler.cs) is a `DelegatingHandler` registered on the `IModelContextProtocolClient` typed HTTP client, slotted after `ModelContextProtocolRoutingHandler` in the pipeline. By the time it runs, the request URI has already been rewritten from the sentinel `http://server-name/` into the registered endpoint, so the loopback-detection logic sees the real host:port.

On every outbound request:

1. **Shutdown short-circuit.** If `IHostApplicationLifetime.ApplicationStopping.IsCancellationRequested` is set *and* the request is a `DELETE` (the MCP session-teardown call `McpClient.DisposeAsync` issues during shutdown), the handler synthesizes a `200 OK` response locally and returns it without going to the listener. The listener is mid-shutdown anyway and would 401 the request, which would then be logged as `MCP shutdown failed.` on the way out — noise that doesn't reflect a real problem.
2. Compare the request URI against the internal listener's URI. If the request port matches *and* the host is `localhost` or a loopback IP literal, treat it as loopback.
3. For loopback requests, read the current agent off [`ICurrentAgentAccessor.Current`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/ICurrentAgentAccessor.cs).
4. Mint a one-shot bearer with `IAgentTokenStore.Issue(agent)` and stamp it onto the request as `Authorization: Bearer <token>`.
5. For non-loopback requests, do nothing — external MCP servers are trusted on their own connection.

The `ICurrentAgentAccessor` scope is opened by `Agent.DispatchToolCallsAsync` *around* the parallel `Task.WhenAll`, so the `AsyncLocal` value flows into every spawned task. If the scope is missing on a live (non-shutdown) loopback call — i.e. an outbound MCP request to the loopback listener with no current agent — the handler throws. This is intentional: it's a programming error, not a runtime condition to recover from. Shutdown is the one place where "no scope on a loopback DELETE" is an expected flow rather than a bug, which is why it's the one branch that's tolerated above.

### Tool discovery

[`ModelContextProtocolToolDiscovery.DiscoverAsync(serverNames, cancellationToken)`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/ModelContextProtocolToolDiscovery.cs) is called once per agent at load (from `AgentManager.DiscoverToolsAsync`). For each server name in the resolved registry it calls `IModelContextProtocolClient.ListToolsAsync(name)`, then wraps the result in a `ToolGroup(Source, Tools)` and accumulates. Per-server failures are logged at `Warning` and the server is dropped from the result so one bad server can't take the whole discovery pass down. There is no "discovery-only" auth path — all outbound MCP traffic shares the same typed client and the same routing+loopback pipeline.

## Why this shape

Three forces shaped the MCP integration:

1. **One protocol for both sides.** Agents talk to remote MCP servers; the host *is* an MCP server. Building tools as `[McpServerToolType]` classes means the same surface that an external server would expose is what the host's own surface looks like — no parallel "internal-only" code path.
2. **Auth boundary at the listener, not the loop.** Tools enforce access in the MCP handler (`IAgentWorkspaceLocator`, future per-tool grants). The agent loop is control flow, never security. A bug in the loop can't accidentally widen access.
3. **Loopback bearer keeps the principal explicit.** Even though every loopback call is in-process, the framework still goes through bearer auth so the `HttpContext.User` is a real `ClaimsPrincipal` on the MCP server side. Tools that read claims work the same regardless of whether the caller is loopback or external.

## Forward-looking

The shared memory index records two pending changes in this area:

- **Bearer-shaped, agent-bound, single-use nonce.** The current bearer is a base64 random token paired to a stored `AgentInfo`. The forward design is a self-describing nonce validated by an ASP.NET Core auth handler that emits a `ClaimsPrincipal` directly — no token store round-trip. The change is mostly invisible to the `AgentBearerHandler` consumers; tools and middleware continue reading claims off `HttpContext.User`.
- **MCP client loopback auth handler as the deployed name.** Today's `LoopbackBearerHandler` is exactly that handler; what's still pending is the cleanup that lets external MCP destinations share the same `HttpClient` with no risk of accidentally injecting a host token outbound.

Both are tracked as "future, not yet built" — don't build against them.
