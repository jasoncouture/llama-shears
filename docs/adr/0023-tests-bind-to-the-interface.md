# ADR-0023: Tests bind to the interface, not the concrete class

Accepted 2026-05-07.

## Context

A unit test that wants to exercise behaviour exposed through an interface has two ways to declare the variable it manipulates:

```csharp
ICronScheduler scheduler = new CronScheduler(...);
// vs
CronScheduler  scheduler = new CronScheduler(...);
```

Both compile. Both run. They are not equivalent.

C# permits a class to expose two methods that look identical at the call site:

```csharp
public sealed class Thing : IThing
{
    public void Foo() { /* concrete-only path */ }
    void IThing.Foo() { /* interface-contract path — what consumers actually hit */ }
}
```

When the test variable is typed as `Thing`, `scheduler.Foo()` resolves to the public method. When it is typed as `IThing`, the same call resolves to the explicit interface implementation. These can be — and often are — different code. Real consumers (DI-resolved through `IThing`) get the second one. A test that bound to `Thing` is exercising a method real consumers never reach. The test passes. The shipped code breaks.

The same hazard shows up without explicit interface implementation when the concrete class adds methods that aren't on the interface, or when a method is virtual on the class but the interface dispatch goes through a different vtable slot. In every case the tell is the same: the test's mental model of "I'm checking the contract" is wrong, because the variable's static type isn't the contract.

A separate but related pressure: Roslyn's **CA1859** ("Use concrete types when possible for improved performance") nudges code in the wrong direction here. Its diagnostic on a test that returns `IThing` from a helper says "change to `Thing` for better perf." The "better" is a virtual-call eliminations win that does not matter in tests, and the change actively introduces the hazard above.

## Decision

In test code, the variable, parameter, and return-type of any helper that hands a service to a test method binds to **the interface**, never the concrete class. The concrete class is permitted only at the construction expression itself and only because there is no interface-side `new`:

```csharp
// OK — construction is concrete, the binding it lives in is the interface.
ICronScheduler scheduler = new CronScheduler(store, agents, time, logger);

// OK — helpers return the interface.
private static ICronScheduler NewScheduler(...) { ... return new CronScheduler(...); }

// NOT OK — variable / return-type / parameter typed as the concrete.
CronScheduler scheduler = new CronScheduler(...);
private static CronScheduler NewScheduler(...) { ... }
```

Concrete-only behaviour (a method on the class that isn't part of any interface) is exercised by a *separate* test that names the concrete class. The mixed case ("here's an interface helper, but I'm reaching past it for one assertion") is a smell that the contract is incomplete or the test is at the wrong layer.

CA1859 is disabled repo-wide in `.editorconfig`. Production code follows the same shape for the same reason, and the perf prompt is not load-bearing for any code we ship.

## Consequences

- Tests fail when the *interface contract* fails, not when an unrelated method on the impl changes. Refactors that move logic between explicit-interface and public methods don't silently bypass the test suite.
- Helpers compose: a test fixture returning `ICronScheduler` is substitutable. A test that wants to swap the impl for a fake doesn't have to update typed locals on every call site.
- Tests that need to reach concrete-only state (private impl members exposed via internals, statistics counters, etc.) become explicit second-class tests with their own helper. They don't quietly hide inside an interface-shaped one.
- CA1859's prompt no longer surfaces. The premature-optimisation pressure is gone; the analyzer's value in non-hot-path code does not justify the safety cost.
- Composes with [ADR-0017 (services come from DI or `ActivatorUtilities`)](0017-services-come-from-di-or-activatorutilities.md): production code resolves through interfaces; tests assert through interfaces; the through-line is a single line of sight.
