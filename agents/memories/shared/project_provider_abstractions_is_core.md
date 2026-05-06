---
name: Provider.Abstractions is the de-facto core/plugin contract
description: LlamaShears.Provider.Abstractions is morally "the core abstractions" and will be published as NuGet for plugin loading via AssemblyLoadContext
type: project
---

`LlamaShears.Provider.Abstractions` is not just provider-side; it is the de-facto core/contract surface that other abstractions are allowed to depend on. The agent abstractions referencing it (`Agent.Abstractions → Provider.Abstractions`) is intentional, not a leak.

The longer arc: `Provider.Abstractions` will be split out and published as a NuGet package so plugins can load via `AssemblyLoadContext`. That detail is deferred — "we can sort that out later" — but it shapes today's decisions.

**Why:** Plugins are the eventual extensibility model. The contract that plugins compile against must be a stand-alone, version-stable, NuGet-shippable assembly. `Provider.Abstractions` is already the lowest-level shared contract (`ModelRole`, `ModelTurn`, `ModelPrompt`, `IProviderFactory`, `ILanguageModel`), so it is the natural home for that boundary even though the name reads narrower.

**How to apply:**
- Treat `Provider.Abstractions` as the project's core contract, not just provider-specific. New shared domain types (`ModelTurn`-shaped things, role enums, conversation primitives) belong here.
- Do **not** preemptively rename it to "Core" / "Domain" / etc., split it, or move types out. The user wants to sort the eventual NuGet split out later, on their schedule.
- Anything added here should be plugin-loadable: no host-internal types, no DI infrastructure, no EF concerns, no runtime services. Pure contracts and DTOs.
- When tempted to add a new abstraction project ("Domain.Abstractions", "Conversation.Abstractions") because the name doesn't fit, raise the option — but expect the answer to be "put it in Provider.Abstractions, that's the core."
- The `Agent.Core → Hosting` edge introduced by `AddAgentManager` is a different rat-nest concern and is not covered by this memory.
