---
name: Prefer collection expressions
description: Use C# collection expressions (`[a, b]`) over `ImmutableArray.Create`, `new List<>()`, `new[] { ... }`, and similar factory calls
type: feedback
---

When constructing collections — arrays, lists, immutable arrays, immutable lists, spans, etc. — prefer the C# 12 collection-expression syntax `[a, b, c]` over factory calls such as `ImmutableArray.Create(a, b, c)`, `new List<T> { a, b, c }`, `new[] { a, b, c }`, or `new T[] { a, b, c }`. Empty collections become `[]`.

Apply to every site that targets a supported collection type, including analyzer-style returns:

```csharp
public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    => [Descriptors.PrimaryConstructorOnNonRecord];
```

**Why:** The user explicitly requested this convention. Collection expressions are the modern, type-target-inferred form; the factory calls are legacy noise that the compiler can produce more efficient code from when given the syntactic form.

**How to apply:**
- New code: always prefer `[...]` when the target type accepts a collection expression.
- Touching existing code: switch to `[...]` opportunistically.
- Don't replace usage that needs runtime composition (e.g. `.Add` after creation, conditional building, or dynamic content) — those stay as builder/list code.
