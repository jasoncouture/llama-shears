# ADR-0002: No public or internal fields

## Status

Accepted (2026-04-27). Enforced by `LS0002` (`NoPublicOrInternalFieldsAnalyzer`).

## Context

Fields and properties expose state, but they are not interchangeable. Reading a field is a direct memory access; reading a property is a method call (a *trivial* method call — the JIT will, in nearly every case, inline auto-property accessors back into a direct memory access, so the runtime cost difference is somewhere between negligible and unmeasurable).

The decisive difference is not performance; it is **ABI stability**. A property looks like a field at the call site but it is, mechanically, a method. Switching a public field to a public property — or adding a getter body, a validation step, lazy initialization, change notification, or anything else that requires *behavior* — is a binary-incompatible change. Every consumer compiled against the field reference has to be recompiled. With a property, the same change is internal to the implementation; the call site is unaffected.

A "public field" decision therefore has two failure modes:

- **You will eventually want behavior.** When you do, the change becomes a coordinated breaking-change exercise across every consumer rather than an internal edit.
- **You won't, but the field tells future readers nothing.** A public field communicates "I am state, with no invariant, that anyone may read or write." That is rarely what you actually mean; usually you meant "this is a read-only view of state I own," which is a property.

Internal, protected, and protected-internal fields suffer the same ABI hazard for any consumer that lives across an assembly or inheritance boundary, just with a smaller blast radius. The argument is the same.

The cultural counter-example here is Unity, where `public` fields drive the Inspector's auto-wiring. That choice has propagated a pattern (public mutable fields as the default expressive vocabulary) into a generation of C# code that has nothing to do with Unity. We do not adopt it.

## Decision

Fields must be `private`. Public, internal, protected, protected-internal, and private-protected fields are forbidden — exposing state requires a property.

`const` fields are exempt. They are compile-time constants, not state, and the ABI argument does not apply: a `const` value is baked into consumers at compile time regardless of accessibility.

The rule is non-configurable; it is enforced as a hard compile error by `LS0002`.

## Consequences

- Every externally visible piece of state goes through a property. Adding behavior later is an internal edit, not a breaking change.
- Trivial getter-only properties are slightly more verbose than a public field. The verbosity is the price of the option to add behavior later.
- Library and framework patterns that expect public fields (notably Unity-style serialization) are not supported in this project. They were not in scope.
- `public const` is permitted and is the right tool for genuine constants.
