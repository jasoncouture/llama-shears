# ADR-0014: Source-generated logging is the default

Accepted 2026-04-28.

## Context

`ILogger.LogDebug("…", arg1, arg2)`-style calls look cheap and aren't. Every call site:

- Boxes value-type arguments into `object[]` for the params array, regardless of whether the log level is enabled.
- Allocates that array on every call.
- Pays the cost of any non-trivial argument expression — `path.GetFullPath()`, `obj.ToString()`, string interpolation passed in instead of a template — even when the message will be filtered out.

The runtime guard inside `ILogger` (`IsEnabled(level)`) avoids the *write*, not the *argument evaluation*. The arguments are evaluated before the call, so the savings only matter if the work to produce the arguments is itself cheap. For a debug log that runs once per file in a directory tree, "looks cheap" stops being true the moment the tree gets large. Roslyn flags this as `CA1873` ("Avoid potentially expensive logging"), and the fix is the same one Microsoft has been pushing since .NET 6: source-generated logging via `[LoggerMessage]` partial methods.

A source-generated log call:

- Skips argument formatting entirely when the level is disabled — the generator emits an `IsEnabled` check first, guarding the body.
- Avoids the params-array allocation: each parameter is positional and strongly typed.
- Caches the message template's `EventId` and structured properties at compile time.
- Costs roughly the same to write as a regular `LogDebug` call, once you've set up the partial method.

The cost imbalance matters most for the categories of logging we want to encourage: debug, trace, and high-volume info. If a debug log inside a hot loop is performance-relevant, developers either downgrade it ("we'll re-enable it when we have a bug") or omit it entirely ("not worth the perf hit"). Both outcomes leave operators with worse observability than they should have. Source-generated logging removes that trade-off — a disabled debug call is essentially free, so liberally adding them is the right call.

The other side of this trade is partial methods. Each call site needs a corresponding `[LoggerMessage]` declaration on a partial class. That is a small, mechanical cost per log site. Code review absorbs it. Modern IDE refactor tooling absorbs more of it. The benefit — a project where "is logging too expensive here?" is never the reason a debug line was omitted — is worth the discipline.

## Decision

**Source-generated logging via `[LoggerMessage]` partial methods is the default for every log call in this codebase.** Direct `ILogger.LogX(…)` calls are a code smell.

The pattern:

```csharp
public sealed partial class TemplateSeedingStartupTask
{
    private readonly ILogger<TemplateSeedingStartupTask> _logger;

    private void Seed(string source, string destination)
    {
        // …
        LogCopiedFile(_logger, target, file);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Copied file {Target} (from {Source}).")]
    private static partial void LogCopiedFile(ILogger logger, string target, string source);
}
```

`CA1873` is enforced as a warning (or higher) project-wide. New code that would trigger it does not pass review.

**Acceptable direct-call cases** are narrow, and each one needs to be a deliberate choice, not a default:

- One-off boot-time messages where the call cost is irrelevant and the log site is genuinely a single line that won't fire again.
- Test code, including in-process test loggers and `NullLogger.Instance`-equivalent shims.
- Generated code, scaffolded code, or third-party patterns that aren't ours to restructure.

When in doubt, the answer is "use the generated method." The generator boilerplate is a few lines; the alternative is litigating performance trade-offs at every log site for the rest of the project's life.

**Naming and placement.** The generated partial methods live on the same class that calls them, named `Log<Verb>` (e.g. `LogCopiedFile`, `LogSkippedSeed`). Static or instance is up to the situation; both are supported by the generator. Keep the `[LoggerMessage]` declaration adjacent to its callers, at the bottom of the type or in a small `#region`. Resist factoring them into a separate "logging companion" class — that's exactly the kind of logging boilerplate this approach is meant to avoid.

## Consequences

- Adding a debug or trace log is cheap by construction. The "is this log going to hurt perf?" question stops appearing in code review for the categories where it matters most.
- Operators get richer logs at no runtime cost on the production happy path. Turning the namespace up to Debug for a session reveals real detail without forcing a rebuild.
- New code carries a small amount of partial-method boilerplate per log site. The IDE and refactor tooling soften this; code review treats it as a normal cost, not a debate.
- `ILogger.LogDebug(…)` and friends in existing code become migration candidates. The migration is mechanical and incremental — no big-bang refactor; replace direct calls when files are touched for other reasons.
- Third-party libraries we consume that use direct `LogX` calls are out of scope for this rule. We are not responsible for their logging style.

This ADR pairs with [ADR-0007 (Pit of success)](0007-pit-of-success.md): the easy logging path becomes the cheap one. Direct `LogX` calls were "easy but secretly expensive" — the kind of friction the pit-of-success principle pushes us to design out. It also pairs with [ADR-0009 (Pragmatism over technical purity)](0009-pragmatism-over-technical-purity.md): the boilerplate cost is real, and the carve-out for one-off boot-time and test calls is the pragmatic side of the bargain.
