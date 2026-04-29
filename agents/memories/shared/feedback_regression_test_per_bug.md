---
name: Every bug gets a regression test
description: When a bug is found and fixed, also write a test that fails on the bug and passes on the fix
type: feedback
---

Any time a bug surfaces and gets fixed, the same change must include a regression test. The test must fail against the broken code and pass against the fix — that's what makes it a *regression* test rather than just a unit test that happens to exist nearby.

**Why:** A bug found once is cheap to write a test for; the same bug found again in six months is expensive to re-diagnose. Regression coverage is the leverage point for not paying that cost twice. The user has stated this directly.

**How to apply:**
- The fix commit (or the immediately-following commit) introduces a test that exercises the exact failure mode. Name and comment make the regression intent explicit — e.g., a `Regression_…` prefix or a comment citing the symptom that motivated it.
- Pick the smallest, most focused test that locks the behavior. Don't bury the regression case in a sweep of unrelated coverage.
- Cover the *contract*, not the bug-fix mechanism. If the bug was "STJ rejected `"High"` because the enum lacked `JsonStringEnumConverter`," the regression test deserializes a real-shape JSON document and asserts the resulting enum value — not "the converter attribute is present."
- Group thematically related regression tests so the suite remains discoverable. Serialization regressions go under `tests/LlamaShears.UnitTests/Serialization/`; future categories follow the same pattern.
- This rule pairs with the project's "tests don't assert on log output" memory. Regression tests assert on observable behavior the code under test promises, not on side effects we don't contract on.
