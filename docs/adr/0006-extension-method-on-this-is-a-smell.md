# ADR-0006: Extension method invoked on `this` is a smell

Accepted 2026-04-27. Surfaced by `LS0006` (`ExtensionMethodOnThisAnalyzer`) as a configurable warning.

## Context

ADR-0001 forbids `this.<member>` access with one mechanical carve-out: the language requires `this.` to invoke an extension method on the current instance, so the qualifier is allowed there.

Allowing the construct is not the same as endorsing it. When a type writes `this.Foo()` to call an extension method `Foo` defined on its own type (or a base type), it is doing something specific: reaching outside the type for behavior that operates on the instance. That is sometimes the right call — third-party domain extensions, intentional layering between policy and mechanism, glue between disjoint subsystems — but it is more often a sign that the behavior in `Foo` *should have lived on the type* and was placed in an extension out of convenience or organizational habit.

This is a smell, not a defect. There is no general rule that catches the bad cases without also catching the good ones, so the appropriate response is a configurable warning: visible by default, easy to suppress at the call site or via `.editorconfig` where the indirection is intentional.

## Decision

`this.<extensionMethod>(...)` produces a `Warning`-severity diagnostic (`LS0006`). The rule is **not** marked `NotConfigurable`; it can be suppressed via:

- `#pragma warning disable LS0006` for a region,
- `[SuppressMessage]` on the surrounding member,
- `dotnet_diagnostic.LS0006.severity = none` (or any other severity) in `.editorconfig`,
- a project-wide `<NoWarn>` for cases where the policy does not apply.

When suppressing, prefer the narrowest scope that fits — call-site suppression is preferable to a project-wide override, because the smell is local.

## Consequences

- Each `this.<extensionMethod>` invocation surfaces in the build log until the author either inlines the behavior onto the type or explicitly suppresses with intent.
- LS0004 (no `this.`) and LS0006 work as a pair: LS0004 keeps the qualifier out of the codebase entirely except where it is mandatory, and LS0006 makes the mandatory cases visible enough to argue with.
- Suppression is a deliberate act and leaves a trail. The pragma, attribute, or .editorconfig entry is itself a documentation point — "we know this is unusual; here is why."
- This rule may surface false positives for codebases that lean heavily on extension-based architectures (LINQ, fluent builders defined externally, etc.). Project-wide suppression is acceptable in those cases and the ADR is fine being revisited.
