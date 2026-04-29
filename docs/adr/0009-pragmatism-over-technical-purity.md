# ADR-0009: Strive for technical purity, where practical

Accepted 2026-04-28.

## Context

Technical purity — consistency, predictability, principled mandates that eliminate whole classes of error — is a real virtue. The project's accepted mandate ADRs are all purity decisions, and each one earns its place by delivering a concrete improvement:

- [ADR-0001 (No `this.` qualifier)](0001-no-this-qualifier.md) and [ADR-0003 (underscore-prefix on fields)](0003-underscore-prefix-for-fields.md) eliminate name-resolution ambiguity at the source.
- [ADR-0002 (No public or internal fields)](0002-no-public-fields.md) eliminates ABI fragility.
- [ADR-0004 (No primary constructors on non-record types)](0004-no-primary-constructors-on-non-record-types.md) eliminates a silent-mutability hazard.
- [ADR-0005 (One top-level type per file)](0005-one-type-per-file.md) eliminates navigation ambiguity.

Each is a "you must do it this way" position the project takes deliberately, because the alternative is measurably worse for the people writing or reading the code. Purity here is not aesthetic; it is load-bearing.

Purity has a failure mode, though: it can be pursued past the point where it serves users. The canonical case is the recurring stance in the Go ecosystem that "all configuration must come from environment variables." It is defensible in the abstract — env vars are widely supported, cleanly namespaced, easy to inject in containers. It is also operationally hostile to a real subset of users: macOS GUI-launched applications cannot easily see environment variables set in shell rc files; `launchctl setenv` is per-session; the workarounds are fragile and surprising. A user on macOS running an Ollama-style daemon discovers that the "right way" to configure the tool does not work, and the project's response — when it has one — is "set up your environment differently" rather than "we should accept config from a file." The technically-pure mandate has crossed into impracticality, and the project has chosen aesthetic consistency over the user's outcome.

The point is not that purity is bad. The point is that purity divorced from practicality stops being purity in any useful sense and becomes aesthetic preference defended with a principle. The mandates that earn their place are the ones that are *both* pure *and* practical.

## Decision

The project strives for technical purity. The default design instinct is the principled, consistent, technically-correct approach. Mandates that eliminate whole classes of error or fragility are a feature, not a problem to solve around.

Purity is gated by practicality. Before accepting a pure design — particularly a mandate that forbids alternatives — the design has to clear this bar:

> Does this impose meaningful friction on a real subset of users in legitimate environments?

If yes, and the constraint is not load-bearing for security, correctness, ABI stability, or another concrete user-facing reason, the pure design is wrong. The user's environment is not the bug; the constraint is.

When a previously-accepted mandate becomes impractical — because the world changes, a new platform emerges, or a use case nobody anticipated arrives — the ADR is revised. A mandate that has crossed into impracticality is a bug in the design, not a fact of the project. Revisiting an accepted ADR is expected behaviour, not a failure of resolve.

Aesthetic consistency without a load-bearing reason is not technical purity. "It's cleaner this way" or "it's the convention" do not by themselves justify a mandate. The accepted mandate ADRs each carry a concrete justification (ABI stability, ambiguity elimination, navigability); new mandates have to do the same.

## Consequences

- The project's defaults are opinionated and pure where pure pays.
- Mandates exist where purity is also practical. Where it isn't, the project provides accommodations and explicitly chooses usability over consistency.
- The bar for proposing a new mandate is "is this technically pure *and* does it not impose meaningful friction on real users in legitimate environments?" Both halves are required.
- A mandate that was right yesterday but isn't right today gets revised. The existing accepted mandates ([ADR-0001](0001-no-this-qualifier.md) through [ADR-0005](0005-one-type-per-file.md)) all currently clear the bar. If any one of them ever stops clearing it, the right response is to revise the ADR, not to defend the constraint.
- Some surfaces will have multiple paths to the same outcome because pure-only would create real friction. The accommodation is intentional.

This ADR pairs with [ADR-0007 (Pit of success)](0007-pit-of-success.md) and [ADR-0008 (No per-model workarounds)](0008-no-per-model-workarounds.md): pit-of-success says the easy path should be the right path; ADR-0008 scopes which class of problems shape that path; this ADR scopes the *kind* of "right" that's worth pursuing — the kind that's both technically defensible and actually usable.
