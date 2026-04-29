## Provider Factory

To support model instantiation and management, a provider factory interface will be introduced:

- The factory interface is responsible for creating model instances.
- The main provider interface is actually a model interface, representing a single model instance that the API and clients interact with.
- This separation allows for flexible model management and supports scenarios where a provider can surface multiple models.
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

## Provider Model

Providers are pluggable components that surface model capabilities to the API layer. The provider model uses a base interface for core functionality, with optional capability interfaces layered on top:

- **Base Interface:**
	- Accepts text and a cancellation token.
	- Returns `IAsyncEnumerable<ModelResponseFragment>` (see below for details).
- **Capability Interfaces:**
	- Each additional capability (e.g., embeddings, chat, image generation) is represented by a separate interface.
	- Providers implement only the interfaces for features they support.
	- This avoids a monolithic interface and enables clear, type-safe feature detection.

This approach ensures separation of concerns, extensibility, and clarity for both provider implementers and consumers.

### ModelResponseFragment

`ModelResponseFragment` is currently an empty record type, serving as a placeholder for future response structure.
