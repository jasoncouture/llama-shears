# ADR-0007: Pit of success

Accepted 2026-04-28.

## Context

Every API, every type, every default, every error message either *guides* the next person to a correct outcome or it nudges them off a cliff. The choice of which is being made constantly, often unconsciously, in every design decision the project takes.

"Pit of success" is a name for the principle that the easiest path through a system should be the correct one. The next engineer (or user) reading the code, calling the API, configuring the tool, or extending the framework should land on the right answer by default — without having to read the documentation, remember a footnote, or apply discipline that depends on always being awake. The cliff edges should require effort to reach: not impossible, but inconvenient enough that the inconvenience itself becomes a signal that something unusual is happening.

The principle is the unifying rationale behind most of the concrete rules already accepted in this project:

- ADR-0001 (no `this.`), ADR-0003 (underscore prefix), ADR-0004 (no primary constructors on non-record types) all collapse name-resolution ambiguity at the source rather than relying on the reader to disambiguate correctly each time. The pit is "the obvious reading is the only reading."
- ADR-0002 (no public fields) makes the easy choice (a property) match the durable choice (ABI-stable, behavior-extensible). The pit is "the default is the right default."
- ADR-0005 (one type per file) makes "open the file named after the type" work. The pit is "tooling guesses correctly."

These analyzer rules are concrete instances of pit-of-success, but the principle is broader than them. It governs:

- API design: required parameters in the constructor, optional ones in builders/setters; types that make invalid states unrepresentable; error messages that point at the next action rather than the failure.
- Defaults: the default value of a config option should be the value most users want; getting a sensible system should not require reading every flag.
- Error surfaces: failures should fail loudly, early, and with information that closes the gap between symptom and cause.
- Documentation: the README should make the first run trivial; the canonical examples should be the ones we wish people copied.
- Tooling: analyzers, types, and structure are preferred over conventions and human discipline, because conventions decay and people get tired.

## Decision

Every design choice in this project is evaluated through the pit-of-success lens. When two otherwise-equivalent designs differ in how easily they are misused, the one that makes misuse harder wins. When elegance and pit-of-success are in tension, pit-of-success wins.

This is a foundational ADR: it informs the others rather than being implemented by any single component. It carries no analyzer because the principle is meta — it tells us *why* analyzers (and types, and APIs, and defaults) are constructed the way they are.

## Consequences

- Up-front design cost goes up. Picking the right default, the right type signature, or the right opinionated API surface takes more thought than picking any default.
- Surface elegance occasionally suffers. Verbose-but-misuse-resistant beats terse-but-trap-laden.
- Opinion is encoded in tooling and types, not in conventions. The project leans on the compiler, the analyzer, and the type system to keep callers on the path. Conventions and code reviews are second-line, not first-line.
- Decisions made *without* the pit-of-success lens are review-time pushback material. "It's allowed" is not a sufficient justification for a design that the next person has to be careful around.
- Future ADRs that argue against this principle are required to be explicit about the trade — which is rarer in practice than ergonomic shortcuts pretend.
