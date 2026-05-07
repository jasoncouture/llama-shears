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
    .WithTools<ReadFileTool>()
    .WithTools<ListFilesTool>()
    .WithTools<WriteFileTool>()
    .WithTools<AppendFileTool>()
    .WithTools<DeleteFileTool>()
    .WithTools<RegexReplaceFileTool>()
    .WithTools<GrepTool>()
    .WithTools<StoreMemoryTool>()
    .WithTools<SearchMemoryTool>()
    .WithTools<IndexMemoryTool>();
```

Tool names follow a `<category>_<action>` convention so they group naturally in the model's tool listing:

| Tool | Purpose |
|------|---------|
| `file_read`, `file_list`, `file_write`, `file_append`, `file_delete`, `file_regex_replace`, `file_grep` | Filesystem operations. Relative paths resolve against the agent's workspace; absolute paths are honored. See *Filesystem tools* below. |
| `memory_store`, `memory_search`, `memory_index` | Memory operations. See [memory.md](memory.md). |

The internal listener is published into the host's outbound MCP registry under the fixed name `llamashears` (see [`ModelContextProtocolServerRegistry.BuildAllKnown`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/ModelContextProtocolServerRegistry.cs)). An agent that whitelists `"llamashears"` (or omits the whitelist) sees the bundled tools as `llamashears__file_read`, `llamashears__memory_store`, etc.

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

When the agent loop has tool calls to dispatch (see [agent-loop.md](agent-loop.md), step 8), it calls [`IToolCallDispatcher.DispatchAsync(ToolCall, ct)`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/IToolCallDispatcher.cs). The implementation is [`ModelContextProtocolToolCallDispatcher`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/ModelContextProtocolToolCallDispatcher.cs).

```
ToolCall(Source = "github", Name = "list_issues", ArgumentsJson = "{...}")
   │
   ▼
ServerRegistry.Resolve(["github"])
   │
   ▼
HttpClient (named "ModelContextProtocol", with LoopbackBearerHandler)
   │
   ▼
McpClient.CreateAsync(HttpClientTransport(serverUri))
   │
   ▼
client.CallToolAsync(name, args)
   │
   ▼
ToolCallResult(FlattenedText, IsError)
```

Per-call rules:

- **Empty `Source`** → reject with an error `ToolCallResult` immediately, without making a network call.
- **Unknown `Source`** → reject with an error result. The model sees the rejection on the next iteration.
- **Server present but unreachable / throws** → catch, log, return error result. The agent loop persists the failure as a `Tool` turn with `IsError = true`.
- **Pooled MCP client.** Each consumer (`ModelContextProtocolToolCallDispatcher` and `ModelContextProtocolToolDiscovery`) holds a `LifetimeCache<(string Name, Uri Endpoint), McpClient>` keyed by server identity. The first call to a server pays the MCP `initialize` / capabilities-exchange handshake; subsequent calls reuse the connection. Idle entries (default 5 min) are evicted from the cache and properly disposed; the consumers themselves implement `IAsyncDisposable` so DI shutdown drains the pool. This replaces an earlier design where every dispatch built and tore down a fresh `McpClient` — the old shape worked but flooded `Information` logs with "client created and connected" / "message processing canceled" lines on every tool call.

### Server registry

[`ModelContextProtocolServerRegistry`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/ModelContextProtocolServerRegistry.cs) merges two sources:

- Operator-configured servers from `ModelContextProtocolOptions.Servers` (`appsettings.json`):

  ```json
  "ModelContextProtocol": {
    "Servers": {
      "github": "https://mcp.example.com/github",
      "internal-tools": "http://10.0.1.5:8080/mcp"
    }
  }
  ```

- The host's own listener, published under the fixed name `llamashears` if `IInternalModelContextProtocolServer.Uri` resolves to a non-null value. The internal URI is read off `IServer.Features` at request time, so it picks up whatever Kestrel actually bound to.

`Resolve(whitelist)` returns:

- The full registry if `whitelist` is `null` (omit `mcpServers` in agent config to opt into everything the host knows about).
- Only the named servers if `whitelist` is non-null. Names that don't resolve are logged at `Warning` and skipped — the agent gets a strictly smaller catalog rather than a hard failure.

### Loopback bearer minting

[`LoopbackBearerHandler`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/LoopbackBearerHandler.cs) is a `DelegatingHandler` registered on each consumer's named HTTP client. The clients are named after the consumer class via `nameof(ModelContextProtocolToolDiscovery)` and `nameof(ModelContextProtocolToolCallDispatcher)` — both at the registration side and the resolution side, so a rename of either consumer carries through automatically and a string-typo can't silently fall back to a default-configured client.

On every outbound request:

1. **Shutdown short-circuit.** If `IHostApplicationLifetime.ApplicationStopping.IsCancellationRequested` is set *and* the request is a `DELETE` (the MCP session-teardown call `McpClient.DisposeAsync` issues during shutdown), the handler synthesizes a `200 OK` response locally and returns it without going to the listener. The listener is mid-shutdown anyway and would 401 the request, which would then be logged as `MCP shutdown failed.` on the way out — noise that doesn't reflect a real problem.
2. Compare the request URI against the internal listener's URI. If the request port matches *and* the host is `localhost` or a loopback IP literal, treat it as loopback.
3. For loopback requests, read the current agent off [`ICurrentAgentAccessor.Current`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/ICurrentAgentAccessor.cs).
4. Mint a one-shot bearer with `IAgentTokenStore.Issue(agent)` and stamp it onto the request as `Authorization: Bearer <token>`.
5. For non-loopback requests, do nothing — external MCP servers are trusted on their own connection.

The `ICurrentAgentAccessor` scope is opened by `Agent.DispatchToolCallsAsync` *around* the parallel `Task.WhenAll`, so the `AsyncLocal` value flows into every spawned task. If the scope is missing on a live (non-shutdown) loopback call — i.e. an outbound MCP request to the loopback listener with no current agent — the handler throws. This is intentional: it's a programming error, not a runtime condition to recover from. Shutdown is the one place where "no scope on a loopback DELETE" is an expected flow rather than a bug, which is why it's the one branch that's tolerated above.

### Tool discovery

[`ModelContextProtocolToolDiscovery.DiscoverAsync(servers, ct)`](../../src/LlamaShears.Core/Tools/ModelContextProtocol/ModelContextProtocolToolDiscovery.cs) is called once per agent at load (from `AgentManager.DiscoverToolsAsync`). For each `(name, uri)` pair in the resolved registry it acquires (or reuses) the cached `McpClient`, lists the server's tools, and translates each into a `ToolDescriptor`. The result is grouped per server (`ToolGroup(Source, Tools)`) and handed to the `Agent` constructor as `ImmutableArray<ToolGroup>`.

Discovery uses its own named HTTP client (`nameof(ModelContextProtocolToolDiscovery)`) and therefore picks up `LoopbackBearerHandler` independently of the dispatcher. Calls to `llamashears` during discovery mint and consume a one-shot token just like every other loopback call. There is no "discovery-only" auth path. Discovery's `McpClient` cache is the same `LifetimeCache` shape as the dispatcher's, so an agent that discovers tools once at load and then makes calls a few minutes later reuses the connection.

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
