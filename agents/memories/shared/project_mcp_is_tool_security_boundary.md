---
name: MCP is the tool security boundary
description: Tool allow/deny enforcement lives in the MCP server's handlers, not in the agent loop or anywhere upstream. The agent has shell access and can hit MCP directly, so any layer above MCP is bypassable.
type: project
---

The MCP server is the *only* place where tool authorization is enforced. Everything else is best-effort UX.

**Why:** Agents have shell access. An agent that wants to call a tool it isn't supposed to can `curl` (or otherwise invoke into) the in-process MCP endpoint directly, bypassing any filtering the agent loop tries to do upstream. Therefore "the agent loop intercepts X before dispatch" is *never* a security claim — it's a control-flow / convenience claim only. The MCP server's `WithListToolsHandler` (advertise gate) and `WithCallToolHandler` (dispatch gate) are the actual boundary, and they must filter using a principal that the framework establishes — not from agent-readable inputs like the URL slug.

**How to apply:**
- When designing tool routing or filtering, the rule "MCP is the boundary" trumps any other layer. If you find yourself building a filter outside the MCP handlers and calling it security, stop.
- The route slug `/mcp/{agentId}` is convenience for routing context; it is *not* the principal. The framework establishes the principal independently (in-proc binding mechanism — owned by the host, not designed in this memory).
- `agent.json` may carry per-agent allow/block lists. Those are *inputs* to the MCP-layer policy, not enforcement points themselves.
- The agent loop's role with respect to tools is control flow only: termination via `ReportStatus`, iteration limits, scope creation, error surfacing. Not authorization.
- Always dual-gate when filtering: filter at `tools/list` (don't advertise) AND at `tools/call` (don't dispatch). Single-gating leaks because a tool name learned out-of-band can still be invoked.
