# ADR-0026: Avoid mutation of data structures that are not local to the function

Accepted 2026-05-14.

## Context

A function that mutates a data structure it does not own produces non-obvious side effects on its callers. The mutation is not announced in the signature, it is not reflected in the return value, and the caller's variable now points at something different than it did one line earlier — without any visible assignment at the call site.

That has a real cost beyond aesthetics:

- **Tracing breaks.** When debugging a transform, the standard move is to log the input, log the output, and diff them. If the input was mutated, the "input" the log captured *is* the output by the time you read it, or has been re-pointed into the same object the output references. The before/after comparison no longer exists; you have to read the function body to know what changed.
- **Aliasing surprises.** A caller that handed a shared collection to two consumers in sequence has no reason to expect the first consumer reshapes what the second one sees. The second consumer's bug report points at the second consumer; the actual fault is in the first.
- **Defensive copies leak in.** `ToList()` mid-iteration, `new Dictionary<,>(source)` before a call, "just in case we modify it" — these patterns are paying for mutation that was never necessary. Eliminate the mutation and the defensive copies go away with it.
- **Reasoning becomes whole-program.** With immutability, the meaning of a value at one line is fixed for the rest of its scope. With ambient mutation, you have to know every function that touched it.

The smell that most often precedes a violation is `foreach (var x in collection.ToList())` where the `ToList()` exists solely to permit `collection.Add(...)` / `collection.Remove(...)` inside the loop. When you see that pattern, the right fix is almost always to build a new collection and return it.

This ADR is about *non-local* mutation specifically. Inside a function, building up a local variable — a `StringBuilder`, a freshly allocated `Dictionary` that will be returned, the result accumulator of a transform — is fine and idiomatic. The line is between data structures the function brought into existence and owns end-to-end, versus data structures handed to the function (parameters, fields, captured locals from an enclosing scope).

## Decision

Functions do not mutate data structures they did not allocate themselves. Concretely:

- Parameters are inputs. Do not `Add`, `Remove`, index-assign, or otherwise alter their contents. Return a new value instead.
- Fields and other captured state are not modified by helpers; the type that owns the state is the only place that may write to it, and even there, prefer building a new value and replacing the field reference atomically over in-place edits.
- Static or otherwise shared state is hands-off from arbitrary call sites by default; any in-place modification of shared state needs an explicit justification (concurrency primitive, performance-load-bearing hot path, etc.) and should be narrow in scope.
- Building up a brand-new container *inside* the function and mutating it locally before returning it is fine. That container's mutation is invisible to anything outside the call.

Mutable objects should be narrow in scope and have a justification. The justification is recorded at the point of use (a comment, an ADR reference, or simply being inside a type whose entire purpose is to manage that mutable state, like a connection pool or an in-memory cache).

When transforming a tree (e.g. `JsonNode`, an expression tree, a domain aggregate), the recursive helper builds a new node at each level and `DeepClone`s leaves it carries over. Recursion does not need a defensive copy of the input — the input is untouched.

This decision is scoped to hand-written code in this repository. Generated code, BCL types, and third-party APIs may have their own mutation conventions; we work with them as they are.

## Consequences

- Helpers become safe to compose without thinking about ordering effects on shared state. Two calls in sequence cannot interfere through their inputs.
- `ToList()` / `new Dictionary<,>(other)` defensive copies disappear from call sites. The patterns that motivated them — "I don't trust this function not to mutate" — no longer apply.
- Some operations cost more in allocations: a copy-on-write merge allocates a new container; an in-place merge does not. In this codebase that cost is below the noise floor of GC pressure we already accept, and the readability win is worth it. If a future hot path needs in-place mutation for performance, that becomes a justified narrow exception rather than the default style.
- The bar for "this function takes a mutable thing and mutates it" rises. If a helper genuinely needs to mutate shared state — the type owns a cache, the type *is* the mutable thing — that intent is clear at the boundary (the mutable type's surface) rather than tucked inside an unrelated helper.
- Tests get easier: an arrange step that builds an input can be reused across multiple act steps because no act step rewrites the input.
- Records and `with` expressions become the default shape for value-like data. `ImmutableArray` / `ImmutableDictionary` are preferred over `List` / `Dictionary` when the collection is part of a shared value's surface.
