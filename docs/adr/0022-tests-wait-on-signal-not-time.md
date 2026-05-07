# ADR-0022: Tests wait on the actual signal, never on `Task.Delay`

Accepted 2026-05-07.

## Context

Asynchronous integration tests need a way to coordinate with the production code under test: send an input, wait until the system has done its work, assert. The lazy way to bridge that wait is `Task.Delay(SomeTimeSpan)` — pause for what the author guesses is "long enough" and then read state.

That guess is wrong on every machine that isn't the one the test was written on. A developer's laptop produces a result in 50ms, the CI runner in 500ms, an oversubscribed shared agent in 3s. Pick the delay short and the test fails on slow infrastructure. Pick it long and the suite drags. Either way, the test is not asserting on the system's behaviour — it is asserting that the system is at least as fast as the author's machine on the day the test was written. That is not a property worth shipping.

The concrete failure that prompted this ADR was the integration tests `UserMessageIsPersistedToCurrentJsonAfterTheAgentResponds` and `AgentDirectoryReturnsPersistedTurnsForUiHistorySeed`. They passed locally and failed on the GitHub Actions runner. Reading the source: the wait helper was `await Task.WhenAny(done.Task, Task.Delay(ResponseTimeout))`, and downstream of that the test expected the persisted file to contain a specific entry. The combination meant the test was racing the persister: when the assistant-turn TCS fired, it raced the persister's file write, and on a slow runner the test read the file before the write landed.

`Task.Delay` is not the only shape this anti-pattern takes. Sleeping in any form — `Thread.Sleep`, busy-loops with `Task.Yield`, polling with a fixed retry count and no condition gate — has the same defect: the test is encoding a guess at how long the system takes, instead of observing what the system did.

This rule is independent of [ADR-0021 (flaky tests are failing tests)](0021-flaky-tests-are-failing-tests.md). ADR-0021 says you can't rerun-green your way out of a race. This ADR says: even when investigating a flake, the fix is never to add a delay or raise an existing one. The two compose: 0021 prevents you from ignoring the failure, 0022 forbids the most common low-effort attempt to silence it.

## Decision

Tests wait on the signal that the production code emits, not on the wall clock.

- **No `Task.Delay`, `Thread.Sleep`, or equivalent sleep in test code.** This includes hidden sleeps inside polling helpers ("retry every 50ms for 5s") that don't observe a real condition between iterations.
- **The signal is whatever the system genuinely produces** — an event on the bus, a `TaskCompletionSource` that the production code completes, a callback the test installs, a file that appears, a property whose value transitions. Pick the most direct one available; if none exists, surface a new one through the production code rather than work around its absence with a sleep.
- **Timeouts are expressed via `CancellationTokenSource`, not via parallel `Task.Delay`.** The canonical shape is:

  ```csharp
  var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
  // ...arrange the production code to call tcs.TrySetResult(...)...
  using var cts = new CancellationTokenSource(timeout);
  var result = await tcs.Task.WaitAsync(cts.Token);
  ```

  `WaitAsync(cancellationToken)` throws `TaskCanceledException` when the token fires; the test's failure message names what it was waiting for. `Task.WhenAny(tcs.Task, Task.Delay(...))` is rejected — it leaves a dangling `Task.Delay` and forces the test to recheck which task won.
- **When the signal is a derived condition** (e.g. "the persister has finished writing"), the test waits on the *production-emitted* event that brackets that condition, not on a polling loop. If the production code doesn't expose a usable signal, that's a production-code defect — fix it there.
- **Multiple cooperating signals** are acceptable. A test that waits for both the assistant turn event *and* a "context persisted" event (registering both, awaiting `Task.WhenAll` of two TCSes, with one CTS-bounded timeout for the pair) is correct. The same test waiting for the assistant turn and then sleeping 100ms "to let the persister catch up" is not.

## Consequences

- Tests stay green on machines slower than the author's. CI passes that depend on per-runner speed disappear.
- The production code accumulates explicit signals (events, completion sources, observable state transitions) where it previously had implicit timing. That's a feature: the same signals are useful for diagnostics and for non-test consumers.
- A test that needs information the production code doesn't emit forces the question "should it?" — sometimes the answer is yes (add the signal), sometimes the answer is "the test's assertion is at the wrong layer." Both outcomes beat "add a sleep."
- The `WaitAsync(token)` shape becomes the house style for awaiting a TCS with a deadline; reviewers reject `Task.WhenAny(tcs.Task, Task.Delay(...))` on sight.
- Tests that legitimately exercise time-dependent behaviour (e.g. a periodic background service) use `TimeProvider` / `FakeTimeProvider` to advance the test's notion of time deterministically, not `Task.Delay`. ADR-0019's mandate that async APIs accept a `CancellationToken` is a prerequisite — without it, the WaitAsync pattern doesn't compose.
- Quarantining a "flaky" test by widening its sleep is explicitly forbidden under this ADR, mirroring ADR-0021's stance on rerun-greening.
