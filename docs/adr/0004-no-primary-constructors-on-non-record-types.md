# ADR-0004: No primary constructors on non-record types

## Status

Accepted (2026-04-27). Enforced by `LS0001` (`PrimaryConstructorOnNonRecordAnalyzer`), with code fix `PrimaryConstructorOnNonRecordCodeFixProvider`. The companion suppressor `LSSPR0001` (`SuppressIde0290`) silences the IDE's "use primary constructor" suggestion that pushes in the opposite direction.

## Context

Primary constructors on records are a well-defined feature: the parameters are the canonical state of the value, the compiler synthesizes init-only properties, and the equality and `with` semantics flow from the parameter list. The feature pulls its weight there.

On non-record classes and structs, the same syntax produces something different and more dangerous. The primary-constructor parameter is **not** a property and **not** a readonly field. It is a captured variable scoped to the entire instance, and it is **always mutable** — the compiler will happily let you assign to it from any instance method. The language gives you no mechanism to make it readonly.

The traps that follow:

1. **Mixed read patterns.** It is common to use the parameter directly *and* to back a readonly field with it: `private readonly int _someVar = someVar;`. Now half the type's code reads the (mutable) parameter and half reads the (immutable) snapshot. If anything, anywhere, mutates the parameter, the two views silently diverge. The bug is not visible at the assignment site; it is visible only at whichever read happens to use the wrong view.
2. **No conventional escape.** The natural way to disambiguate would be to name the parameter `_someVar`, matching the field convention (ADR-0003). But constructor parameters are *parameters*, and project convention reserves `_` for fields. Renaming the parameter to dodge the ambiguity violates a different rule.
3. **Ambiguity, again.** This is the same root cause that drives ADR-0001 (no `this.`) and ADR-0003 (underscore prefix): two symbols in overlapping scopes that the language permits but the reader has to disentangle.

The IDE's IDE0290 ("use primary constructor") suggestion actively pushes code into this trap. Suppressing it (`LSSPR0001`) is part of the same decision.

## Decision

Primary constructors are forbidden on non-record `class` and `struct` declarations. Records (`record`, `record struct`) are unaffected — the feature was designed for them and works correctly there.

The rule is non-configurable; it is enforced as a hard compile error by `LS0001`. The companion code fix converts an offending type into an explicit constructor with one `private readonly _name` field per parameter and rewrites parameter references inside the body to the corresponding field. A primary-constructor base call (`: Base(x)`) is forwarded to a `: base(x)` initializer on the generated constructor.

## Consequences

- Non-record types declare an explicit constructor. The boilerplate is real but mechanical, and the code fix automates the conversion.
- The "captured mutable parameter masquerading as state" failure mode does not exist in this codebase.
- Records keep the feature where it is sound.
- The IDE will not pester us to introduce primary constructors via IDE0290; that suggestion is suppressed.
