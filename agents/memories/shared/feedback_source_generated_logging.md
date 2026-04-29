---
name: Source-generated logging is the default
description: Use [LoggerMessage] partial methods for log calls; direct ILogger.LogX is a code smell (CA1873)
type: feedback
---

Every log call in this codebase uses a `[LoggerMessage]`-generated partial method by default. Direct `_logger.LogDebug(…)` / `LogInformation(…)` / etc. calls are a code smell.

The pattern:

```csharp
public sealed partial class Foo
{
    private void DoStuff() => LogStuffDone(_logger, count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stuff done: {Count}.")]
    private static partial void LogStuffDone(ILogger logger, int count);
}
```

**Why:** Codified in [ADR-0014 (Source-generated logging is the default)](../../../docs/adr/0014-source-generated-logging-is-the-default.md). Direct `LogX` calls allocate a params `object[]` and evaluate their arguments unconditionally — the runtime `IsEnabled` check guards the write, not the argument evaluation. The generator emits an `IsEnabled` gate and skips formatting entirely when the level is disabled, which makes liberal debug/trace logging effectively free on the production happy path.

**How to apply:**

- New log call → reach for `[LoggerMessage]` first. The class becomes `partial` if it isn't already; the generated method is named `Log<Verb>` and lives at the bottom of the same class as its caller. Don't hoist them into a separate "logging companion" class — that's the boilerplate this approach is meant to avoid.
- Existing direct `LogX` calls → migrate when you touch the file for other reasons. No big-bang sweep.
- Acceptable direct-call cases (deliberate, not default): one-off boot-time messages where the call cost is genuinely irrelevant, test code (including `NullLogger.Instance` shims), generated/scaffolded code, third-party patterns.
- CA1873 ("Avoid potentially expensive logging") is the analyzer signal. Treat hits as the same bar as a build warning.
- `[LoggerMessage]` requires the partial method to be `static` *or* an instance method — both are supported. Static is usually less ceremony; pass the `ILogger` as the first parameter.
