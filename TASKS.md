# Tasks

Capture file for ideas and followups that surface during a session and need a
home before they drift. Latest at top. Trim freely — anything done or no
longer relevant comes out.

## OpenAI provider
**Surfaced:** 2026-05-07

Mirror `LlamaShears.Provider.Ollama`'s shape for an OpenAI-compatible
provider. `llama-server` (ships with llama.cpp) exposes `/v1/chat/completions`,
`/v1/completions`, `/v1/embeddings`, and `/v1/models` natively, so the same
provider also covers any OpenAI-API-compatible local server (vLLM, LM Studio,
TabbyAPI, etc.) — one provider, many backends.

Open questions:
- Per-agent base URL (so an agent can target a specific local server) vs
  host-level default?
- Tool-call surface — OpenAI's function-call schema vs Ollama's flatter shape;
  pick one and adapt the dispatcher, or carry both.
