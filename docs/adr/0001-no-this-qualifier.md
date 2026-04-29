# ADR-0001: No `this.` qualifier

Accepted 2026-04-27. Enforced by `LS0004` (`NoThisQualifierAnalyzer`).

## Context

Every `this.<member>` falls into one of two buckets:

1. **It resolves a collision or naming conflict.** A parameter or local shadows a field or property, and `this.` is required to bind to the member instead of the local symbol. The qualifier is load-bearing — but only because two symbols were given overlapping names in the first place. The right fix is to rename, not to paper over the conflict at every call site.
2. **It is noise.** No collision exists; the bare identifier would resolve to exactly the same symbol. The qualifier carries no information.

Either way, `this.` is unnecessary: case (1) is a name choice that should have been made differently, and case (2) is decoration. The `_`-prefix field convention (ADR-0003) makes case (1) structurally impossible — fields begin with `_`, parameters and locals do not, so a method-scope name can never shadow a field.

There is one mechanical exception: calling an extension method on the current instance requires `this.Foo()` because the compiler does not implicitly bind extension methods to `this`. The qualifier there is a language-imposed requirement, not a stylistic choice. Even so, an extension call from inside the receiver's own type is itself worth questioning — behavior that operates on the instance usually belongs on the instance.

## Decision

`this.<member>` is forbidden in this codebase, with one exception: when `this.` qualifies the receiver of an extension-method invocation, where the qualifier is mandatory.

The rule is non-configurable; it is enforced as a hard compile error by `LS0004`.

Bare `this` (e.g. `return this;`, `ReferenceEquals(this, other)`) is unaffected — it is a value reference, not a member-access qualifier.

## Consequences

- Field/parameter shadowing cannot be papered over with `this.`; it must be resolved by renaming. Combined with ADR-0003, no shadow can occur in the first place.
- Reviewers do not have to decide "is this `this.` load-bearing or noise?" — every `this.` in the codebase is load-bearing (extension call) or it is a build break.
- Calling an extension method on the current instance via `this.Foo()` remains legal, but the qualifier itself is a code-review trigger to ask *why* the type is delegating to an external static rather than owning the behavior.
