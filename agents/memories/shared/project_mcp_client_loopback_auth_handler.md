---
name: MCP HTTP client loopback auth via DelegatingHandler
description: Where the future agent MCP HTTP client's per-request bearer attachment lives — a DelegatingHandler that detects loopback-to-our-listener and injects the current agent's nonce, leaving external destinations untouched.
type: project
---

When the agent-side MCP HTTP client lands, the natural place for nonce attachment is a `DelegatingHandler` registered on the `HttpClient` the agent uses for tool calls. Per-request decision tree:

1. Inspect outbound `RequestUri`: host is `localhost` or `IPAddress.IsLoopback(host)`, and port equals our API listener's bound port.
2. If yes → internal call. Resolve the current agent (from the framework's per-call context — `AsyncLocal` or DI scope, TBD), mint a nonce via `IAgentTokenStore.Issue(agent)`, set `Authorization: Bearer <nonce>` on the request.
3. If no → external MCP server. Leave the request alone (or apply whatever per-server auth config the operator set up — separate concern).

**Why:**

- The agent never sees its own bearer. Token minting + attachment happens entirely below the agent's reach.
- Internal vs external auth routing is determined by destination URI, not by the agent or by a flag the agent could set. Removes the failure mode where an agent's outbound external call accidentally leaks our nonce, and the failure mode where we forget to attach the nonce on internal calls.
- The `Authorization` header collision concern (custom-header vs bearer, raised when designing the principal) collapses here: a stock HTTP client targeting an external MCP server works untouched, our internal target gets the bearer, and they never compete for the same header on the same request.
- One handler is the only code that needs to know about nonce minting on the outbound side. The MCP client SDK consumes a normal `HttpClient`; everything else is invisible.

**How to apply (when this lands):**

- Register the handler on the agent's tool-call `HttpClient` only — not on every `HttpClient` in the host (`HttpClientFactory` named/typed clients make this easy).
- The listener's bound port must be reachable to the handler. After app start, read `IServer.Features.Get<IServerAddressesFeature>()` and cache the loopback addresses + port in a singleton the handler can inject.
- Match permissively on host: literal `localhost`, plus `IPAddress.IsLoopback(parsed)` for `127.0.0.1` / `::1` / any in `127.0.0.0/8`. An agent-supplied URL targeting any of these forms still gets recognized.
- The handler is *one* of several places that could attach the bearer; this memory documents that it is the correct one. Don't make individual call sites attach the token.
