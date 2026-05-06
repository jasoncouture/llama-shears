# ADR-0021: Flaky tests are failing tests

Accepted 2026-05-05.

## Context

A flaky test is one that fails non-deterministically — it passes most of the time, fails some of the time, and the failure usually does not reproduce on a rerun. The reflexive industry response is to retry: re-run the test, and if it passes the second time, count the suite as green.

That response treats the symptom (CI red) as the problem. The actual problem is that the test surfaced a real fault — a race, a leaked resource, a disposal-order bug, an ordering assumption that does not hold — and the production code under test contains that same fault. The test is the only thing that caught it, and "rerun until green" is the act of throwing away the only signal that surfaces the bug. The bug stays in production code; it just stops getting reported.

The cost of this stance shows up in two places:

- **Production incidents trace back to flakes that were rerun-greened.** Once "this one is flaky" enters team folklore, the same failure mode in production is harder to recognize because the team has been trained to dismiss the signal.
- **Trust in the test suite erodes.** A suite where a real failure looks indistinguishable from "just a flake" is a suite the team stops reading. Real failures get rerun alongside the flakes, and eventually nobody investigates anything.

The retry stance is also asymmetric in the wrong direction. Investigating a flake costs hours; ignoring one costs nothing today and an unbounded amount later when the underlying race fires in production. The discount-rate math only works if you assume the underlying bug never matters — and the entire reason the test caught it is that the bug *can* matter.

The concrete pattern that prompted this ADR: an integration test failed at host shutdown with `KeyNotFoundException` from MessagePipe's `FreeList<T>.Remove`. It passed clean on rerun. The temptation was to call it a flake. Investigation showed `EagerCompactor.StopAsync` was disposing its subscription handles non-idempotently, and the host calls `StopAsync` more than once during some teardown paths, so the second dispose hit an already-removed FreeList entry. Real bug. Real fix. Rerun-greening it would have left the same race in shipped code — visible only as occasional production shutdown noise that nobody ever traced back.

## Decision

Flaky tests are failing tests. There is no separate category. A test that fails non-deterministically is treated, triaged, and prioritized identically to a test that fails every time:

- The CI signal is "failed." It does not get downgraded by being retried into green.
- Reruns are a diagnostic tool — used to gather information about reproducibility — not a remediation. A green rerun does not close the issue.
- The expected outcome is a fix to the underlying race, ordering bug, or resource-lifetime bug. "Made the test more lenient" (added a sleep, raised a timeout, retried inside the test, weakened the assertion) is not a fix; it is making the bug invisible.
- If the root cause genuinely is the test infrastructure (clock skew, shared port, parallel-isolation gap), the fix is in the infrastructure. The bar is the same: identify the cause, fix the cause, leave evidence in the diff.
- Quarantining a flake — disabling it, marking it `[Skip]`, excluding it from CI — is permitted only as an explicit, time-boxed step toward the fix, recorded in the issue tracker with an owner and a target. A quarantined test that has no owner and no target is a deleted test that is still consuming review attention; either fix it or remove it.

The corollary: the suite is allowed to be loud. A test that catches a one-in-fifty race is doing exactly what a test should do. The response is to fix the race, not to make the test stop reporting it.

## Consequences

- CI is taken at face value. Red is red. The "rerun and move on" workflow is off the table.
- Investigating a non-deterministic failure is part of the work, not optional cleanup. Time budgets for changes that introduce or surface flakes account for this.
- Pressure shifts onto code-under-test: races, disposal-order issues, and lifetime bugs are forced into the open early instead of being absorbed into team folklore.
- Tests stay believable. A failure carries weight because it is not coexisting with rerun-greened noise.
- Some classes of bug — typically concurrency and shutdown bugs — surface during test runs as flakes. Under this ADR, those flakes are the suite earning its keep, not friction to be smoothed over.
- A small number of tests will, in practice, depend on infrastructure outside the project's control (network, external services). Those are not flakes; they are integration tests with explicit external dependencies, marked as such, run on a separate signal, and excluded from the unit/integration suite that gates CI. The line is intent: "depends on a thing that can be down" is fine and labeled; "depends on a race we have not yet found" is a bug.
