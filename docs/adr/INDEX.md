# Architectural Decision Records

This directory captures the project's significant architectural and policy decisions in lightweight ADR form. Each entry records the context that drove the decision, the decision itself, and the consequences we accept by adopting it.

ADRs are accepted by definition — if a record is not yet accepted, it is a proposal and does not live here. If a decision changes, write a new ADR that supersedes the old one rather than editing the original. Each ADR carries the date it was accepted on its title line.

## Index

1. [No `this.` qualifier](0001-no-this-qualifier.md) — Analysis ID LS0004
2. [No public or internal fields](0002-no-public-fields.md) — Analysis ID LS0002
3. [Field names must start with `_`](0003-underscore-prefix-for-fields.md) — Analysis ID LS0003
4. [No primary constructors on non-record types](0004-no-primary-constructors-on-non-record-types.md) — Analysis ID LS0001
5. [One top-level type per file](0005-one-type-per-file.md) — Analysis ID LS0005
6. [Extension method invoked on `this` is a smell](0006-extension-method-on-this-is-a-smell.md) — Analysis ID LS0006 (configurable warning)
7. [Pit of success](0007-pit-of-success.md)
8. [No per-model workarounds](0008-no-per-model-workarounds.md)
9. [Strive for technical purity, where practical](0009-pragmatism-over-technical-purity.md)
10. [Exception handling requires explicit justification](0010-exception-handling-requires-justification.md)

## Format

Each ADR follows the [Michael Nygard format](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions):

- **Context** — What forces are at play; what problem is this addressing.
- **Decision** — The position we are taking.
- **Consequences** — What becomes easier and what becomes harder as a result.
