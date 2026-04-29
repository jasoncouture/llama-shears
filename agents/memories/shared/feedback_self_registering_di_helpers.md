---
name: Self-registering DI helpers
description: Item-registration extension methods should pull their companion infrastructure (runners, interceptors, hosted services) in themselves
type: feedback
---

When an extension method registers an item that needs a companion piece of infrastructure to do anything (a runner, dispatcher, interceptor, hosted service, etc.), the item-registration method calls the infrastructure-registration method itself. Callers register the item; the infrastructure follows for free.

This works because the infrastructure registrations are written to be idempotent — `services.AddHostedService<T>()` (which is just `TryAddEnumerable` for `IHostedService`), `services.TryAddEnumerable(ServiceDescriptor.X<...>())`, etc. Calling them N times costs nothing after the first.

**Why:** Eliminates the silent failure where the user adds tasks/hooks/handlers and forgets the one-line "and now register the runner" call, leaving the items registered but never executed. Removes the wiring trap by construction, not by documentation.

**How to apply:**
- Reach for this pattern whenever a "register one of these" helper exists alongside a "register the thing that consumes them all" helper. Have the per-item helper call the consumer helper.
- Verify the companion registration uses idempotent helpers (`TryAdd*`, `TryAddEnumerable`, `AddHostedService<T>`). Idempotency is the default in this codebase but not absolute — if a registration is intentionally non-idempotent, that's a deliberate choice, not something to "fix" without asking. If it's accidentally non-idempotent, fix that first; making it idempotent is a prerequisite, not a follow-up.
- Reference: `LlamaShears.Hosting.HostingServiceCollectionExtensions.AddHostStartupTask<T>` calls `AddHostStartupTaskRunner` as its first line for exactly this reason.
- Same logic applies in other DI patches in this codebase (e.g. `AddDatabaseHook<T>` could pull the interceptor registration if it doesn't already; check before adding new fan-in helpers).
