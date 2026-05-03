# ADR-0018: Invoke delegates explicitly via `.Invoke`

Accepted 2026-05-03. Surfaced by `LS0014` (`ExplicitDelegateInvocationAnalyzer`) as a hard error, with a paired code fix.

## Context

C# lets a delegate-typed identifier be called two ways:

```csharp
handler(envelope, cancellationToken);          // direct call syntax
handler.Invoke(envelope, cancellationToken);   // explicit invoke
handler?.Invoke(envelope, cancellationToken);  // explicit invoke, null-safe
```

The two forms compile to the same IL, but the source-level difference is significant for a reader. With direct call syntax, `handler(...)` is visually indistinguishable from a method call. The reader cannot tell — without navigating to the declaration — whether `handler` is:

- a local function,
- an instance method,
- a static method,
- a property whose value is a delegate,
- a delegate-typed parameter or field.

Each is invoked with the same syntax. The only ways to disambiguate are tooling (go-to-definition, hover) or naming conventions. Both are friction the reader is forced to absorb.

The explicit `.Invoke` form removes the ambiguity at the call site. `handler.Invoke(...)` can only be a delegate. `handler(...)` is then exclusively a method or local function. Cognitive load drops to nothing — the syntax itself answers the question.

This complements the project's existing naming conventions in a useful way:

- `someName()` is unambiguously a method or local function.
- `someName.Invoke()` is unambiguously a delegate-valued identifier (parameter, field, property, local).
- The same separation extends to properties: `Foo()` is a method member, `Foo.Invoke()` is a delegate property.

The framework already has substantial cognitive cost; reducing it where the language gives us a free lever is worth a hard rule.

## Decision

Delegate invocations must use explicit `.Invoke` (or `?.Invoke` for nullable delegates). Direct call syntax on a delegate-typed identifier produces an `LS0014` error (`NotConfigurable`).

The analyzer scope is narrow:

- It flags only invocations whose semantic target is a delegate's `Invoke` method (Roslyn's `MethodKind.DelegateInvoke`).
- It does not consider naming, identifier casing, or local function shape — those are separate concerns and out of scope here.
- It explicitly does not flag `handler.Invoke(...)` or `handler?.Invoke(...)` — the rule's whole purpose is to make those the canonical form.

The paired code fix rewrites the violation:

- nullable delegate → `handler?.Invoke(args)`,
- non-nullable delegate → `handler.Invoke(args)` (avoids changing the result's nullability for `Func<...>`, which `?.Invoke` would do).

## Consequences

- Reading any `name(args)` expression no longer requires resolving "is this a delegate?" — it isn't. Delegates always announce themselves with `.Invoke`.
- A consistent local convention pairs with this rule: `camelCase()` is a local function or method, `camelCase.Invoke()` is a delegate-valued identifier. The same applies to PascalCase members.
- A small keystroke cost at every delegate call site. Trivial against the readability gain.
- Existing direct-call sites must be migrated. The code fix handles the mechanical conversion; the only judgment call is null-safety, which the fix derives from the static nullability of the receiver.
- Refactors that change a method into a delegate-valued property (or vice versa) now produce a visible change at every call site, not a silent same-syntax substitution. That visibility is a feature.
