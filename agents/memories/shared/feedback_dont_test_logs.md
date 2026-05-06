---
name: Don't test log output
description: Tests must not assert on log content; the only requirement for logs is that they don't throw
type: feedback
---

Tests in this codebase do not validate log output. No assertions on log messages, levels, structured properties, or call counts. The only requirement on a logger from a test's perspective is that calling it does not throw an exception.

**Why:** Log output is observability for humans/operators, not a contract the code under test promises. Asserting on log content turns logs into part of the API surface, freezes message wording, and creates false failures every time someone tweaks a log line. The user has stated this preference directly.

**How to apply:**
- Don't write `_logger.Received().LogX(...)` mocks, fake logger sinks, or `LogEntry` assertions.
- If a test happens to need an `ILogger<T>`, pass `NullLogger<T>.Instance` (or `NullLogger.Instance` for the non-generic) and move on.
- Don't add `services.AddLogging()` to a unit test setup just so logs work — most of the time, transitive dependencies (e.g., `AddHttpClient`) already wire it. If they don't, `NullLogger` covers the dependency without any registration.
- If a test is verifying that *something logged* is the only observable effect of a code path, the test is wrong — find a real observable to assert on, or delete the test.
- Reviewers should treat any new "assert on log" pattern as a smell.

The contract of a logger inside the code under test is "you can call me without crashing." Anything else is overreach.
