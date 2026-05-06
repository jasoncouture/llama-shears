---
name: Always use DateTimeOffset
description: Project-wide preference — every timestamp type is `DateTimeOffset`, never `DateTime`, even when the source API returns `DateTime`
type: feedback
---

Use `DateTimeOffset` for every timestamp in this codebase. `DateTime` is not used, even when:

- The framework API returns `DateTime` (`FileInfo.LastWriteTimeUtc`, `Environment.TickCount` derivatives, etc.) — convert at the boundary: `new DateTimeOffset(file.LastWriteTimeUtc)`.
- The `Kind` is already `Utc` and ambiguity is "impossible" — use `DateTimeOffset` anyway. Consistency across the codebase outweighs the local micro-optimization.
- The value is internal-only (private struct fields, dictionary keys, etc.) — same rule. Internal scope doesn't earn an exception.

**Why:** Codified in [ADR-0013 (`DateTime` requires explicit justification)](../../../docs/adr/0013-datetime-requires-justification.md). `DateTimeOffset` carries the offset, round-trips cleanly through serialization, and survives a process moving between time zones — `DateTime`'s `Kind` flag is a long-standing source of bugs that this rule eliminates by construction. Existing code in the project already uses `DateTimeOffset` (see `ICreated.Created`, `ILastModified.LastModified`, `ModelTurn.Timestamp`, `SystemTick.At`).

**How to apply:**
- Default to `DateTimeOffset` for any new field, parameter, or local that holds a timestamp.
- When calling a framework API that returns `DateTime`, convert immediately: `new DateTimeOffset(dt)` (works for `Kind = Utc` or `Local`; `Unspecified` will throw, which is the right failure).
- Don't store `DateTime` in private structs "because it's just internal" — when reviewing existing code, flag any `DateTime` as a likely bug.
- The only acceptable `DateTime` use is the immediate result of a framework API, held only long enough to wrap into `DateTimeOffset`.
