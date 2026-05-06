# Architectural Decision Records

This directory captures the project's significant architectural and policy decisions in lightweight ADR form. Each entry records the context that drove the decision, the decision itself, and the consequences we accept by adopting it.

ADRs are immutable once accepted: if a decision changes, write a new ADR that supersedes the old one rather than editing the original. The `Status` line reflects the *current* state of each record.

## Index

| #    | Title                                                       | Status    | Enforced by |
|------|-------------------------------------------------------------|-----------|-------------|
| 0001 | [No `this.` qualifier](0001-no-this-qualifier.md)           | Accepted  | LS0004      |
| 0002 | [No public or internal fields](0002-no-public-fields.md)    | Accepted  | LS0002      |
| 0003 | [Field names must start with `_`](0003-underscore-prefix-for-fields.md) | Accepted | LS0003 |
| 0004 | [No primary constructors on non-record types](0004-no-primary-constructors-on-non-record-types.md) | Accepted | LS0001 |
| 0005 | [One top-level type per file](0005-one-type-per-file.md)    | Accepted  | LS0005      |

## Format

Each ADR follows the [Michael Nygard format](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions):

- **Status** — Proposed, Accepted, Deprecated, or Superseded. Date of last status change.
- **Context** — What forces are at play; what problem is this addressing.
- **Decision** — The position we are taking.
- **Consequences** — What becomes easier and what becomes harder as a result.
