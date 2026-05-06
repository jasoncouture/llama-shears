# ADR-0013: `DateTime` requires explicit justification

Accepted 2026-04-28.

## Context

`DateTime` and `DateTimeOffset` are not interchangeable, and the difference is not a stylistic preference. `DateTime` carries a value plus a `Kind` flag — `Utc`, `Local`, or `Unspecified` — and that flag is lossy by design. A `DateTime` returned from one boundary, passed through several functions, and serialized at another boundary frequently arrives with a `Kind` that the receiver cannot trust:

- A `DateTime` with `Kind.Local` is meaningful only on the machine that produced it. Move the process across time zones (or just deserialize on a different host) and the wall-clock value drifts in ways the receiver cannot detect.
- A `DateTime` with `Kind.Unspecified` is semantically a 64-bit integer with a calendar shape. Its meaning depends entirely on a convention the type does not encode and the compiler cannot enforce. Most timestamp bugs in .NET land here.
- A `DateTime` with `Kind.Utc` is fine in isolation, but the `Kind` is not preserved across many serializers, ORM mappings, and round-trips. "It was UTC when I stored it" is a hope, not a guarantee.

`DateTimeOffset` resolves this by carrying the offset alongside the value. The offset is part of the data, not a flag. Round-trips through serialization, persistence, and process boundaries preserve the absolute instant. A `DateTimeOffset` deserialized on a different host means the same point in time it meant on the producer.

The cost of using `DateTime` consistently in a codebase is a class of bugs that surface late and look unrelated to time handling — a log line off by an hour, a database row whose "created at" reads correctly on one machine and shifted on another, a comparison that succeeds on the dev box and fails in production. Those bugs are nearly free to prevent up front and disproportionately expensive to chase down later.

The codebase already encodes this preference at every existing timestamp surface — `ICreated.Created`, `ILastModified.LastModified`, `ModelTurn.Timestamp`, `SystemTick.At`, `SaveChangesHookContext.UtcNow`, the EF interceptors, and so on, are all `DateTimeOffset`. New code that introduces `DateTime` breaks that consistency without a corresponding benefit.

## Decision

**`DateTimeOffset` is the default timestamp type. `DateTime` requires justification.**

A field, parameter, property, return type, struct member, dictionary key, or local variable that holds a point in time uses `DateTimeOffset`. This applies whether the value is a public API surface or a private implementation detail; "it's just internal" is not an exception, because internal types graduate to public ones and private fields end up in serialized payloads.

The only acceptable use of `DateTime` is as the immediate, unwrapped result of a framework or third-party API that returns one (`FileInfo.LastWriteTimeUtc`, `XmlConvert.ToDateTime`, etc.), held only as long as it takes to convert at the boundary:

```csharp
var fingerprint = new FileFingerprint(
    new DateTimeOffset(file.LastWriteTimeUtc),
    file.Length);
```

The `DateTime` exists for one expression. It does not flow into a field, return, or further call.

A `DateTime` member or parameter is permitted only when:

1. The author can articulate, in code review, why `DateTimeOffset` is wrong for the case in question. "Wrong" means actively incorrect, not "slightly less convenient." Examples that *might* qualify: an interop boundary with a third-party library whose contract is `DateTime`-shaped and whose `Kind` semantics are documented; a date-only value that genuinely has no time-of-day component (and which would be better served by `DateOnly` anyway).
2. The decision is recorded — either in a comment at the declaration, or in a follow-up ADR if the case recurs.

If neither applies, the type is wrong and should be `DateTimeOffset`.

`DateTime.Now` and `DateTime.UtcNow` are direct calls to a framework API; treat them like any other `DateTime`-returning API. Use `DateTimeOffset.UtcNow` as the standard "current instant" call. `DateTime.Now` (with `Kind.Local`) should never appear in this codebase — it is always wrong for persistent or shared state.

`DateOnly` and `TimeOnly` (introduced in .NET 6) are the right choice when the value genuinely has no time-of-day or no date component. Reach for them when applicable; they are not covered by the `DateTimeOffset` rule and do not need justification.

## Consequences

- Timestamps round-trip cleanly through JSON, EF Core, and process boundaries. The "what was the `Kind` of this thing again?" question stops appearing in code review.
- Boundary conversions (`new DateTimeOffset(dt)`) become a visible, auditable event. Any `DateTime` in a diff is a flag for review.
- Code review has a concrete question to ask of any new `DateTime` declaration: "why is this not `DateTimeOffset`?" The author who cannot answer is asked to change the type.
- Comparisons across the codebase work without `Kind` checks. `DateTimeOffset` instances compare on the absolute instant, regardless of offset.
- Framework APIs that return `DateTime` (`FileInfo.LastWriteTimeUtc`, `Process.StartTime`, etc.) are still fine to call; the wrapping is mechanical and one line.
- A small amount of memory is spent per timestamp on the offset bytes. This is not a meaningful cost in any program where timestamps appear at human-perceivable granularity.

This ADR pairs with [ADR-0007 (Pit of success)](0007-pit-of-success.md): the pit-of-success path is the one where the type system makes the easy choice the correct one. `DateTimeOffset` makes "use the obvious type" the same as "preserve the absolute instant." `DateTime` makes those two things diverge silently.
