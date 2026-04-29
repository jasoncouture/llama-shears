# ADR-0003: Field names must start with `_`

Accepted 2026-04-27. Enforced by `LS0003` (`FieldMustStartWithUnderscoreAnalyzer`).

## Context

Without a naming distinction between fields and method-scope identifiers, three name pools collide in the same scope: instance fields, parameters, and locals. C# resolves the resulting overlaps with well-defined rules — locals shadow parameters, parameters shadow fields, `this.` reaches past the shadowing — but every reader has to keep those rules in their head while reading the code. The cognitive cost is small per occurrence and large in aggregate.

Prefixing fields with `_` removes the overlap structurally. A bare identifier is a parameter or local; an `_`-prefixed identifier is a field. There is no shadow, no precedence question, and no need for a qualifier (see ADR-0001) to disambiguate. The reader does not have to think about resolution; the resolution is encoded in the spelling.

`const` fields are excluded from the rule. They are compile-time constants, not state, and the convention for constants is `PascalCase`. Including them under the underscore rule would force constants to be spelled like fields, which the rest of the C# ecosystem does not do and which would erase the visual distinction between "constant value" and "instance state."

`static readonly` fields are *not* excluded. The runtime treats `static readonly` as state — it is allocated, initialized at type-init time, and (for reference types) the reference is fixed but the referent is not necessarily immutable. It is closer to "shared state" than "constant" and it follows the field convention.

## Decision

Every field name must start with `_`. The single exception is `const`.

The rule is non-configurable; it is enforced as a hard compile error by `LS0003`.

## Consequences

- Fields, parameters, and locals are visually distinguishable at a glance.
- Field/parameter shadowing is structurally impossible in this codebase, which combines with ADR-0001 to eliminate the legitimate use case for `this.`.
- `static readonly` fields look like fields, not constants. Code that wants the constant *appearance* must use `const` (when possible) or live with the convention.
- Migrating code from other codebases requires a mechanical rename of fields. The cost is a one-time refactor per imported file.
