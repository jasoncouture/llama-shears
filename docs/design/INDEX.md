# Design Overview

This project is a heartbeat-based agentic host, structured in two main layers:

## 1. API Layer
- Contains all core logic and functionality.
- Exposes a pluggable interface to support various providers.
- The only out-of-the-box provider is `ollama`.

## 2. Clients Layer
- Consists of simple frontends that interact with the API.
- No business logic; all intelligence resides in the API layer.

This separation ensures extensibility and maintainability, allowing new providers and clients to be added with minimal friction.
