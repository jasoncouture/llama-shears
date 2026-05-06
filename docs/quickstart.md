# Quickstart

Five minutes from `git clone` to talking to an agent. Two paths:
[Docker Compose](#docker-compose-path) (recommended) or
[`dotnet run`](#dotnet-run-path) (active development).

You will need:

- An Ollama instance reachable on the network with at least one chat model pulled (e.g. `ollama pull llama3.1`) and one embedding model if you want memory (e.g. `ollama pull embeddinggemma`).
- Either Docker (Compose path) or the .NET 10 SDK (`dotnet run` path).

## Docker Compose path

1. **Point at your Ollama.** Drop a `.local.env` next to `compose.yaml` with the Ollama URL:

    ```env
    Providers__Ollama__BaseUri=http://your-ollama-host:11434/
    ```

    (`__` maps to the `:` config separator. Compose loads this automatically.)

2. **Bring it up.**

    ```sh
    docker compose up --build
    ```

    The host serves on `http://localhost:5125`. The default data root is `./.data` — agents, workspaces, and persisted context land there.

3. **Add an agent.** Drop a JSON file at `./.data/agents/claudia.json`:

    ```json
    {
      "model": { "id": "OLLAMA/llama3.1:latest" },
      "embedding": { "id": "OLLAMA/embeddinggemma:latest" },
      "mcpServers": ["llamashears"]
    }
    ```

    The filename (without `.json`) is the agent id. The agent picks up automatically on the next system tick — no restart.

4. **Open the chat UI** at `http://localhost:5125`, select `claudia`, talk.

## `dotnet run` path

1. **Override the Ollama URL** in `src/LlamaShears/appsettings.Development.json` (or via env vars on the launch profile):

    ```json
    {
      "Providers": {
        "Ollama": { "BaseUri": "http://your-ollama-host:11434/" }
      }
    }
    ```

2. **Run the host.**

    ```sh
    dotnet run --project src/LlamaShears
    ```

    Default data root is `~/.llama-shears/`. Override `Paths:DataRoot` to relocate.

3. **Add an agent** at `~/.llama-shears/agents/claudia.json` (same JSON as above).

4. **Open the chat UI** at the launch-profile URL (typically `http://localhost:5125`).

## What just happened

- `LlamaShears.csproj` is the host. It boots an event bus, an MCP server at `/mcp`, the agent manager, and a Blazor chat UI.
- The agent manager scanned `<Data>/agents/*.json` on startup, loaded `claudia.json`, and stood up an `Agent` instance fed by a per-agent channel.
- Your messages from the UI publish onto the bus as `channel:message:<channelId>`; the agent consumes from its channel, runs inference via the Ollama provider, and streams `agent:message:claudia` fragments back to the UI.

## Where to go next

- [Architecture overview](design/architecture.md) — the projects, dependencies, and where state lives.
- [Agent configuration](design/agent-config.md) — the full JSON schema, defaults, reload behavior.
- [Agent workspace](design/agent-workspace.md) — the per-agent overlay (`BOOTSTRAP.md`, `IDENTITY.md`, `MEMORY.md`, etc.) and how the framework consumes it.
- [Eventing](design/eventing.md) — the pub/sub bus everything else rides on.
