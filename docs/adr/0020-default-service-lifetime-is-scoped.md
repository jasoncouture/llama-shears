# ADR-0020: Service lifetime defaults to Scoped

Accepted 2026-05-03.

## Context

`IServiceCollection` offers three lifetimes — Singleton, Scoped, and Transient — and the choice of a default heavily steers the codebase. The common .NET convention is "Singleton unless you need Scoped." That convention has a directional problem.

Singleton-by-default makes future per-operation isolation a retroactive refactor. When a service later needs scope-bound state (a request id, a tenant context, a per-conversation cache, a unit of work, a correlation id), it has to be detangled from singleton consumers that captured a single instance at startup. The discovery is usually a bug ("all operations share state they shouldn't"), and the fix touches every singleton in the dependency chain that holds the captured reference.

The two directions are not symmetric:

- **Scoped → Singleton** is mechanical. Audit shows the service has no per-scope state, change the registration, done. Existing consumers are unaffected: resolving a singleton through a scope still yields the same singleton.
- **Singleton → Scoped** is invasive. Every singleton (or longer-lived) consumer that holds a reference now holds a stale or shared one. Each consumer must be re-examined: become scoped itself, or resolve from `IServiceScopeFactory` per operation. Often resolved by introducing factories or scope helpers — boilerplate that would not have existed if the service had been scoped from the start.

Transient is also a poor general default. A transient registered into a scoped or singleton consumer is captured *once* — the consumer holds a single instance for its own lifetime. That defeats the "fresh on every resolve" intuition without warning. Transient is the right answer for "I always want a brand-new throwaway, AND the consumer also creates fresh ones"; it is not a defensible fallback.

Scoped sits in the middle. Within a scope, the same instance is reused — no allocation churn for the common case where a scope is one request, one tick, one operation. At scope boundaries, state is naturally discarded. And when an audit later confirms a service is genuinely scope-independent, promotion to Singleton is a one-line change.

## Decision

Default service registrations are Scoped. Singleton and Transient are used deliberately, with a reason that holds up to review:

- **Singleton** for services that are genuinely process-bound and either stateless or hold only thread-safe, immutable state. The MessagePipe publisher/subscriber pair is the canonical example: it represents a long-lived dispatch channel, holds no per-scope correlation, and the "scoped publisher" alternative would force a static singleton inside the implementation to share dispatch state — the worst of both worlds, especially for testing.
- **Transient** for the narrow case where the consumer must observe a fresh instance on every resolve *and* the consumer either resolves via a factory or is itself transient. Not a fallback.
- **Scoped** for everything else: services that might eventually want scope state, services whose statelessness has not been independently audited, services whose scope-or-singleton question has not been thought through. Default-Scoped makes the answer to the unconsidered question the safe one.

This implies an operational rule alongside the registration rule: code that resolves services runs inside a scope. Background loops, hosted services, and long-running tasks open their own scope and hold it for the operation's duration, not the host's. ASP.NET request handling, MessagePipe filter pipelines (configured with `InstanceLifetime.Scoped`), and per-tick agent processing already follow this; new code does too.

There is a measurable performance cost. Scoped resolution is slightly heavier than singleton resolution; per-scope dictionary churn is real; allocation at scope boundaries is real. The position is that this cost is paid in profiled hot paths, not as the default — and when it shows up, the fix is the easy direction (Scoped → Singleton, post-audit) rather than the hard one.

## Consequences

- New registrations are `services.AddScoped<...>()` by default. Reviewers expect to see Scoped, not Singleton, in the absence of a stated reason.
- Promoting a service to Singleton requires evidence: holds no scope-sensitive state, never will, and the sharing or perf benefit is real. The promotion is a one-line change once the audit is done.
- Demoting Singleton to Scoped — the painful direction — becomes rare because the default no longer pushes toward Singleton in the first place.
- Transient becomes a special-case lifetime, not a fallback.
- A small registration-time cost (scope tracking, per-scope dictionary) on every resolve. Accepted as a default; optimize when profiling says so.
- Code that resolves services outside of any scope — e.g. resolving directly off the root `IServiceProvider` in a startup-time singleton — will fail when those services are scoped. That is the correct failure: the code should not have been resolving from the root provider. The fix is to open a scope.
