# ADR-0025: `CA1822` (mark members as static) is disabled repo-wide

Accepted 2026-05-09. Suppressed via `<NoWarn>$(NoWarn);CA1822</NoWarn>` in the root `Directory.Build.props`.

## Context

`CA1822` flags any instance method that does not read instance state and recommends marking it `static`. The compiler can dispatch a static call slightly cheaper than an instance call, hence the rule.

That micro-optimization is not what bites in practice. What bites is the churn the rule creates during ordinary refactors:

- A method starts as instance because it logically belongs to the type. CA1822 fires once the body happens to not touch `this` for a release or two.
- The author "fixes" it to `static`. Every call site outside the type now reads `Type.Method(...)` instead of `instance.Method(...)`.
- A later change adds an instance-state read inside the method. The method has to flip back to instance, and every external call site changes again.
- The opposite churn happens when an instance dependency moves out of the body — back to static, back through the call sites.

Each oscillation is mechanical, but it shows up in diffs, in code review, and in cherry-picks. The "savings" CA1822 promises is paid for in maintenance noise that compounds on a long-lived codebase.

For *property* members the trade-off is different. Promoting an instance property to static is rare and usually accompanies a larger refactor — converting it to a method, replacing it with a field, or reworking the calling site. The author is already rewriting the line. CA1822 on properties does not generate the same drip-feed churn that it does on methods, so the property-side guidance is not what we are objecting to.

The rule is also blind to the type's design intent. A method on a type with no instance state at all (a static helper class) wants to be static and CA1822 is correct there. The compiler / analyzer can't tell those cases apart from "method that happens to not touch `this` this week", so we get noise on both.

## Decision

`CA1822` is suppressed repo-wide via the root `Directory.Build.props` `<NoWarn>` list. Methods are static when the author intentionally wrote them static (typically inside a static class, or as a deliberate helper with no logical relationship to instance state). Methods are not made static *because the analyzer asked*.

This decision is scoped to the project's hand-written code. Generated code, test fixtures, and analyzers themselves continue to follow whatever convention they ship with — the project-wide `<NoWarn>` simply removes the warning from our build output.

## Consequences

- Diffs caused by static / non-static oscillation disappear from refactor changesets.
- The trivial perf gain CA1822 chases is forfeited. In a managed codebase like ours that does not run in a hot inner loop, this is below the noise floor.
- Authors who want a method to be static still write `static`. The rule no longer nudges them either way; the choice is intentional or it is not made.
- New contributors will not see the warning in their IDE / build output for this codebase. The rule still exists in the BCL analyzer set; we have only suppressed it for our build, not deleted it.
- If a future situation makes the call-site form load-bearing (for example, a subset of code where every method really should be static for a specific reason), narrow re-enablement via `.editorconfig` or per-project `<NoWarn>` overrides is fine. Re-enabling repo-wide reverses this ADR and should come with a new record.
