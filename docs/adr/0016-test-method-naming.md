# ADR-0016: Test method names use PascalCase

Accepted 2026-04-29.

## Context

Existing tests in this repository were authored with `snake_case` method names (`Issue_returns_a_non_empty_base64_token`). The intent — long, sentence-like descriptions — is sound; the casing is not. `snake_case` identifiers are out of step with the rest of the project's C# code, which is uniformly PascalCase, and they mix poorly with assembly-qualified names in test reports, IDE breadcrumbs, and stack traces (`MyTests.method_under_test_does_thing` reads worse than `MyTests.MethodUnderTestDoesThing`).

The argument for `snake_case` in tests is that underscores improve readability of long names. In practice, capital letters do the same job: `WaitForInputAsyncCompletesImmediatelyIfMessagesAreAlreadyPending` parses fine. The cost of dual conventions across the project — every reader switching mental modes when crossing the test boundary — is greater than the marginal readability gain inside a single test name.

## Decision

Test method names use **PascalCase**.

- Each "word" of the description starts with a capital letter; there are no underscores.
- Multi-word technology terms keep their canonical casing: `JavaScript`, `Json`, `Html`, `Async`, `Utc`.
- 2+ letter acronyms are treated as words, not shouted: `Ollama`, not `OLLAMA`; `Url`, not `URL`. (This matches the [.NET runtime guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/capitalization-conventions) for identifiers longer than two characters.)
- Numbers stay numeric: `Returns200`, not `ReturnsTwoHundred`.
- Tests written specifically to guard a known bug carry the `Regression` prefix and a comment block above them describing the bug they're guarding against (per the "every bug gets a regression test" rule).

Examples:

| Bad                                                  | Good                                                  |
| ---------------------------------------------------- | ----------------------------------------------------- |
| `Get_blazor_web_js_returns_200_javascript_payload`   | `GetBlazorWebJsReturns200JavaScriptPayload`           |
| `Tick_with_no_input_does_not_invoke_the_model`       | `TickWithNoInputDoesNotInvokeTheModel`                |
| `Real_OLLAMA_provider_is_not_resolvable_under_test`  | `RealOllamaProviderIsNotResolvableUnderTest`          |

There is currently no analyzer for this rule; it is enforced by code review.

## Consequences

- New and modified tests follow PascalCase. The convention applies uniformly across unit and integration test projects.
- Existing `snake_case` tests are not grandfathered indefinitely; a sweep renames them in atomic commits separate from any behavioural change. Until the sweep lands, the codebase carries a mix; reviewers should not flag the legacy names as new violations.
- Test names get shorter on the wire (no `_` characters) and align visually with method names elsewhere in the project. Reports, traces, and IDE navigation benefit from the consistency.
- Long descriptions remain encouraged. The convention does not push tests toward terse names — `WaitForInputAsyncCompletesImmediatelyIfMessagesAreAlreadyPending` is the right shape; `WaitForInputAsyncWorks` is not.
