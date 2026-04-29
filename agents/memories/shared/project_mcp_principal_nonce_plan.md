---
name: MCP principal will be a bearer-shaped in-process nonce, validated to claims
description: Future direction for how the MCP server identifies the calling agent. Today the URL slug is the placeholder principal; later it's a single-use in-proc nonce carried as Authorization Bearer, validated by an ASP.NET Core authentication handler that emits a ClaimsPrincipal.
type: project
---

The URL slug `/mcp/{agentId}` is the current placeholder for the calling agent's principal. It is **not** security — it is just routing context that the server trusts because nothing else exists yet.

**Future direction (not implemented, do not build yet):**

- The framework generates a nonce in-process at the moment it dispatches a tool call from an agent. The agent never sees the nonce — the framework's HTTP client owns it and inserts it into the outbound request.
- The nonce travels in the standard `Authorization: Bearer <nonce>` header. **Not** a custom `X-Llama-Shears-*` header. Reasoning: the wire format is plain bearer auth; whether the bearer was minted as a one-shot in-proc nonce or an OAuth 2.1 access token is an implementation detail of the auth handler, invisible to MCP and to tool code.
- An ASP.NET Core `AuthenticationHandler` validates the nonce and emits a `ClaimsPrincipal` (claims include at minimum `agent_id`; potentially tool-grant claims, scopes, etc.). The MCP endpoint requires authentication.
- Single-use: consumed by the auth handler when validated, never honored again.
- Agent-bound: the nonce is minted for a specific agent; validation produces that agent's claims principal.
- Validation happens in-process on both ends (mint and verify in the same process).

**Why bearer-shaped (not custom header):**

- The `WithListToolsHandler` / `WithCallToolHandler` policy code, and any tool that wants caller context, reads `HttpContext.User` — the standard `ClaimsPrincipal` ASP.NET Core authentication produces. No custom `IAgentPrincipal` abstraction needed.
- Tools that don't care about identity get a sensible `ClaimsPrincipal` for free.
- If the project ever supports external OAuth 2.1-authenticated MCP clients, register a second authentication scheme alongside the nonce scheme — both produce `ClaimsPrincipal`, policy and tool code stay identity-source-agnostic. The nonce mechanism evolves in isolation.
- Constraint that justifies this: LlamaShears does not re-host or proxy external MCP servers behind its own endpoint. The bearer header on an inbound request to our MCP is therefore always *our* token; no two-tokens-at-once collision concern.

**Why:** Agents have shell access, so any agent-readable principal (URL slug, agent-controlled header, anything in `agent.json`) can be forged by `curl`-ing the local MCP endpoint. The framework needs an identity proof that the agent itself never possesses.

**How to apply:**
- Don't build the nonce mechanism yet. The user explicitly deferred it. Slug-as-proof is "good enough" until tools start doing anything sensitive.
- When the time comes: build it as a standard ASP.NET Core `AuthenticationScheme`. The MCP server, the policy handlers, and the tools never see the auth machinery — they read `HttpContext.User`.
- Once auth lands, the URL slug `/mcp/{agentId}` becomes optional — claims carry the agent id. Keep it for routing/log convenience or drop it; not load-bearing.
- The same MCP-as-boundary rule still applies (see `project_mcp_is_tool_security_boundary.md`) — claims just upgrade the principal, they don't move the boundary.
