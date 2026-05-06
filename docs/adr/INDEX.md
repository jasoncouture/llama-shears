# Architectural Decision Records

This directory captures the project's significant architectural and policy decisions in lightweight ADR form. Each entry records the context that drove the decision, the decision itself, and the consequences we accept by adopting it.

ADRs are accepted by definition — if a record is not yet accepted, it is a proposal and does not live here. If a decision changes, write a new ADR that supersedes the old one rather than editing the original. Each ADR carries the date it was accepted on its title line.

## Index

- **ADR-0001** — [No `this.` qualifier](0001-no-this-qualifier.md) — Analysis ID LS0004
- **ADR-0002** — [No public or internal fields](0002-no-public-fields.md) — Analysis ID LS0002
- **ADR-0003** — [Field names must start with `_`](0003-underscore-prefix-for-fields.md) — Analysis ID LS0003
- **ADR-0004** — [No primary constructors on non-record types](0004-no-primary-constructors-on-non-record-types.md) — Analysis ID LS0001
- **ADR-0005** — [One top-level type per file](0005-one-type-per-file.md) — Analysis ID LS0005
- **ADR-0006** — [Extension method invoked on `this` is a smell](0006-extension-method-on-this-is-a-smell.md) — Analysis ID LS0006 (configurable warning)
- **ADR-0007** — [Pit of success](0007-pit-of-success.md)
- **ADR-0009** — [Strive for technical purity, where practical](0009-pragmatism-over-technical-purity.md)
- **ADR-0010** — [Exception handling requires explicit justification](0010-exception-handling-requires-justification.md)
- **ADR-0011** — [Config root lives in the user profile](0011-config-root-location.md)
- **ADR-0012** — [XML doc comments default to absent](0012-xml-doc-comments-default-absent.md)
- **ADR-0013** — [`DateTime` requires explicit justification](0013-datetime-requires-justification.md)
- **ADR-0014** — [Source-generated logging is the default](0014-source-generated-logging-is-the-default.md) — `CA1873`
- **ADR-0015** — [Provider workarounds are absorbed, model workarounds are not](0015-provider-vs-model-workarounds.md)
- **ADR-0016** — [Test method names use PascalCase](0016-test-method-naming.md)
- **ADR-0017** — [Services come from DI, or via `ActivatorUtilities`](0017-services-come-from-di-or-activatorutilities.md)
- **ADR-0018** — [Invoke delegates explicitly via `.Invoke`](0018-explicit-delegate-invocation.md) — Analysis ID LS0014
- **ADR-0019** — [Async methods take a non-optional `CancellationToken`](0019-async-methods-take-non-optional-cancellation-token.md)
- **ADR-0020** — [Service lifetime defaults to Scoped](0020-default-service-lifetime-is-scoped.md)

## Superseded

- **ADR-0008** — [No per-model workarounds](0008-no-per-model-workarounds.md) — Superseded by [ADR-0015](0015-provider-vs-model-workarounds.md) on 2026-04-28.

## Lifecycle

ADRs move through three states; the active list and the Superseded section above are the first two.

1. **Active.** The decision is in force.
2. **Superseded.** Demotion happens the moment a replacement ADR is accepted — the entry moves out of the active list and into the Superseded section in the same change that adds the new ADR. The original ADR file stays on disk at this stage; only the index entry moves.
3. **Pending removal.** Superseded ADRs are eventually moved to a Pending removal section. The cleanup pass after that deletes the ADR file from disk and removes the index entry entirely. Git history carries the full record once an ADR is gone from `docs/adr/`.

Cleanup triggers on count, not on release cadence: when the combined Superseded + Pending removal set grows past *X* entries, do a cleanup pass that demotes Superseded → Pending removal and removes Pending removal → disk. *X is currently undefined.* Either pick a number and write it here, or accept that the trigger is maintainer judgment and name it as such — leaving X as an unbound variable means cleanup will never fire.

Rationale: the index is a working surface. Superseded entries are useful for a short window after the supersession (paper trail, in-flight references), but the value of carrying them inline drops off fast. Git carries the long-term record without cost; the index is for what is currently load-bearing.

## Format

Each ADR follows the [Michael Nygard format](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions):

- **Context** — What forces are at play; what problem is this addressing.
- **Decision** — The position we are taking.
- **Consequences** — What becomes easier and what becomes harder as a result.
