# ADR-0017: Services come from DI, or via `ActivatorUtilities` against the container

Accepted 2026-04-29.

## Context

Hand-wired `new MyService(dep1, dep2, dep3, …)` call sites are the classic shape that DI exists to remove. Each one:

- Couples the construction site to every constructor parameter, so adding a new dependency means updating every caller — including ones that have no business knowing the dependency exists.
- Forces the parent class to itself take every transitive dependency a child needs, just so it can pass them along. Constructors balloon. Field lists balloon. Reading the parent tells you nothing about *its* job — it's a transit station for the children's deps.
- Drifts out of sync with the registered DI graph: if the registration changes (a singleton becomes scoped, a new optional dep is added with a default), every manual `new` site needs to be hunted down and updated. The compiler will catch missing parameters; it will not catch lifetime mismatches or registrations that should have been picked up automatically.
- Hides what a class actually depends on, because the explicit param list doesn't include things the manually-instantiated child reaches for. Reviewing a constructor change becomes "and what else does the thing it builds need?"
- Defeats the point of `ServiceProvider` — the container is the source of truth for "how do I make a `Foo`," and routing around it puts that knowledge in N places.

`AgentManager.BuildAgentAsync` was a concrete example: it took `ISystemPromptProvider`, `TimeProvider`, `IConversationStore`, `ILoggerFactory`, two MessagePipe publishers, a subscriber — most of them just to forward into a manually-constructed `Agent`. The manager didn't *use* most of those for itself.

The standard library already has the right tool: `Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<T>(IServiceProvider, params object[] runtimeArgs)`. It resolves constructor parameters from the container by default and lets the caller supply specific values for parameters the container can't know (per-instance configuration, runtime-derived ids, etc.). Logger categories with appended discriminators are the common case for `ILogger` of constructed instances — `loggerFactory.CreateLogger($"{typeof(T).FullName}:{instanceName}")` pairs naturally with `ActivatorUtilities`.

## Decision

**Services are constructed by the DI container.** A class that participates in the application's object graph must be reachable in one of two ways:

1. **Resolved from `IServiceProvider`** — the default. Register it (`AddSingleton`, `AddScoped`, `AddTransient`, `TryAddSingleton`, etc.) and consume it via constructor injection or `IServiceProvider.GetRequiredService<T>()`.
2. **Constructed via `ActivatorUtilities.CreateInstance<T>(serviceProvider, runtimeArgs)`** — for cases where some constructor parameters are runtime values that the container cannot supply (per-instance ids, per-instance config snapshots, things that vary with each call to the factory). The container fills in the rest.

Manual `new MyService(…)` against types that have non-trivial dependencies is **not allowed**. If a callsite does this, either it's a bug to fix or the author must justify why neither path above worked.

**Carve-outs.** This rule is about services and dependencies, not about every `new` in the codebase. The following remain ordinary `new`:

- Primitive and BCL value/utility types: `StringBuilder`, `List<T>`, `Dictionary<,>`, `CancellationTokenSource`, `Stopwatch`, etc.
- Records, DTOs, and value-shaped types whose role is to carry data (`ModelTurn`, event records, configuration snapshots).
- Test fakes and helpers in test projects, where DI plumbing would obscure the test.
- Framework-handed instances — e.g., the framework instantiating a `WebApplicationBuilder` in `Program.cs`.

If a type sits in that ambiguous zone (it has dependencies, but feels too small to register), prefer registration. The cost of a registration line is one line; the cost of a manual instantiation that later sprouts dependencies is N callsites.

**Justification path when neither DI nor `ActivatorUtilities` fits.** Pair the manual construction with a comment that names the constraint:

```csharp
// Justification for manual new: the container cannot resolve <T> at this
// stage because <reason>. Future move to <approach> once <condition>.
var thing = new Thing(arg1, arg2);
```

The justification has to be something a reviewer can challenge. "It's simpler" is not enough — DI's purpose is exactly the bookkeeping it's saving you from.

**Logger categories on constructed instances.** When `ActivatorUtilities.CreateInstance<T>(sp, ...)` is used to build per-instance services that want a discriminated logger category (e.g., one `Agent` per loaded config, each tagged with its agent id), supply an `ILogger` constructed via `ILoggerFactory.CreateLogger($"{typeof(T).FullName}:{name}")` as one of the runtime args. This keeps the category prefix consistent with `ILogger<T>` while making per-instance log filtering possible.

## Consequences

- Constructors stop being transit stations. A class lists what it actually uses; deps that exist only to forward to a child go away.
- Adding a dependency to a leaf type is a one-line change at the type's constructor and the registration. Parent classes that build the leaf type via `ActivatorUtilities` are unaffected.
- Lifetime decisions live in the registration call, not in callers. Changing a singleton to scoped is a one-line change.
- A small amount of `ActivatorUtilities` boilerplate appears at factory sites that have to mix container-resolved and runtime-supplied parameters. This is a known and accepted cost; it is cheaper than the alternatives this ADR exists to avoid.
- The justification carve-out exists so this rule does not become a blocker when a real exception arises (interop with code we don't own, perf-critical hot path where the container's overhead is measurable). The bar is "name the constraint and the path forward," not "permanent escape hatch."
- This ADR pairs with [ADR-0007 (Pit of success)](0007-pit-of-success.md): the easy path becomes the right one. Manual `new` of a service with three deps is *more* code than `ActivatorUtilities.CreateInstance<T>(sp, runtimeArg)`, not less.
- It also pairs with [ADR-0009 (Pragmatism over technical purity)](0009-pragmatism-over-technical-purity.md): the carve-outs above and the justification path are the pragmatic edge of the rule, so the principle does not turn into ceremony for ceremony's sake.
