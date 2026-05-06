# ADR-0010: Exception handling requires explicit justification

Accepted 2026-04-28.

## Context

Exception handling is overdone in most codebases. The reflexive `try/catch` block — wrap the call, log the exception, swallow it, return a default — is a habit that looks like robustness and is actually the opposite. Each one of those blocks:

- Loses information. Stack traces are richest at the point of throw; every layer that catches and rethrows (or worse, swallows) discards context.
- Hides the failure. The caller continues with a default value or `null` and discovers the real problem several frames or several seconds later, in a place where the original cause is impossible to recover.
- Lies in code review. A reviewer reads `catch (Exception ex) { _logger.LogError(ex, "..."); }` and assumes the handler is doing something meaningful. It isn't.
- Decouples failure from response. The block catches the exception in a layer that has no idea what to do about it, *because* the layer that knows what to do is somewhere up the stack — and now never gets the chance.

The right abstraction layer for handling an exception is the layer that knows what to do about it. A connection failure inside a tool is not the tool's problem; it is the retry layer's problem, or the user-facing error reporter's, or the agent loop's. A tool that catches its own connection exception and returns a sad default is making an architectural decision the tool has no information to make correctly.

## Decision

Exceptions propagate by default. The default state of any function in this codebase is "no `try`, no `catch`, no defensive wrapping." Exceptions reach the layer that knows how to handle them, and that layer is rarely the one that produced the call.

A `try/catch` is a deliberate act, not a reflex. To introduce one, the author must be able to answer two questions explicitly:

1. **Why is *this* layer the right place to handle the exception?** "Because something might throw" is not an answer. "Because I have the information here that I can't recover anywhere else" is.
2. **What does the catch block do, concretely?** Real handling: a retry, a fallback that the rest of the code will treat as success, a structured error returned to the caller, a translation to a domain exception, or a deliberate decision to stop. Logging and continuing is not handling.

If neither question has a clean answer, the catch block is wrong and should not exist.

**Tools generally do not handle their own exceptions.** A tool's job is to do its work and throw if it cannot. Retries belong in a retry layer; user-facing reporting belongs in the agent loop or the error reporter; transient-vs-permanent classification belongs in whichever layer makes routing decisions. A tool that swallows its own exceptions is robbing the layers above it of the ability to make those decisions correctly.

**Swallowing is forbidden** — defined as catching an exception and neither rethrowing, nor logging-and-failing, nor taking a meaningful action — unless the reasoning is articulated in the catch block, in writing, and survives review. The rare cases where it is correct (cooperative cancellation, expected-and-uninteresting framework signals) are obvious from context once they are identified.

A `// Ignored` (or equivalent short comment) is acceptable only when the reasoning is obvious to anyone reading the code. The canonical example is a background service catching `OperationCanceledException` while honouring its stopping token at shutdown:

```csharp
try
{
    await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    // Ignored — expected on shutdown.
}
```

That comment is acceptable because *every* reviewer reads it the same way. A `// Ignored` on `catch (Exception)` is not, ever — there is no reading of "ignore arbitrary failures" that is obvious.

## Consequences

- Stack traces stay rich and informative. Failures are loud at the point they occur.
- Exception-handling code is rare. Where it exists, it is intentional and reviewable.
- Tools and other low-level components stay simple. They throw; the layers above them decide what to do.
- Code review has a concrete question to ask of any new `catch`: "why here, and what does it do?" An author who cannot answer is asked to remove the block.
- Defensive `try/catch` patterns copied from other codebases do not survive review.
- Background services and similar long-lived components still need to handle cooperative cancellation and shutdown signals; those are a known, named class of cases and remain straightforward.

This ADR pairs with [ADR-0007 (Pit of success)](0007-pit-of-success.md): the pit-of-success path is the one where failures surface immediately at the layer that can act on them. Catching at the wrong layer breaks that path.
