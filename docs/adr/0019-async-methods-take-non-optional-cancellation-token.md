# ADR-0019: Async methods take a non-optional `CancellationToken`

Accepted 2026-05-03.

## Context

C# permits async methods to declare a `CancellationToken` parameter with a default value:

```csharp
public async Task DoWorkAsync(CancellationToken cancellationToken = default)
```

The convenience is real, and so is the cost:

- A call site that writes `await DoWorkAsync()` is visually identical to a call to a method that genuinely takes no cancellation parameter. There is no signal at the call site that cancellation was skipped — the code reads as "this operation is not cancellable."
- Defaults bias callers toward not threading a token. Plumbing a real `CancellationToken` through a chain of awaits is a small effort; relying on the default is zero. The path of least resistance produces silent uncancellable surface.
- Long-running operations, request scopes, host shutdown, and Blazor/SignalR teardown all depend on cancellation propagation. A single defaulted parameter mid-chain cuts the propagation, and the gap is invisible at every call site above it.
- When the symptom shows up — a request that won't cancel, a host that hangs on shutdown, a tight loop that ignores Ctrl+C — locating which frame dropped the token is harder than it should be, because every default-using call site looks identical to a non-cancellable one.

The mechanism is fine. The ergonomics push toward the wrong default.

## Decision

Async methods declare `CancellationToken` as a non-optional parameter:

```csharp
public async Task DoWorkAsync(CancellationToken cancellationToken)
```

Callers either thread a real token through, or pass `CancellationToken.None` explicitly. The explicit `None` at the call site is itself the documentation: "the author considered cancellation here and chose to skip it."

Scope:

- `Task`-, `Task<T>`-, `ValueTask`-, `ValueTask<T>`-returning methods.
- `IAsyncEnumerable<T>`-returning methods — the `[EnumeratorCancellation]` parameter is not optional.
- `async` methods regardless of return shape (e.g. async iterators).

Synchronous methods that happen to take a `CancellationToken` (short polling loops, etc.) are out of scope; this ADR is about async-shaped surface.

Existing methods that violate the rule are migrated as they are touched. There is no churn pass; the rule is "no new offenders, and fix-as-you-edit."

## Consequences

- Every async method advertises its cancellation contract in its signature. Callers must engage with it, even if the engagement is "I have nothing to cancel with right now."
- `CancellationToken.None` at a call site is a deliberate, visible act. It is also a review hook: "is there really nothing to cancel here, or did a token get dropped on the way down?" That question is now askable from the diff.
- Threading tokens becomes the path of least resistance once any leaf requires one. The chain above must supply a token, and cancellation propagates by default rather than by remembering.
- A small boilerplate cost for tests, scripts, and one-shots where `default` would have been silent. `CancellationToken.None` is six characters of documentation; the cost is acceptable.
