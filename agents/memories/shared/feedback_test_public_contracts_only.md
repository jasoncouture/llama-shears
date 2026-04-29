---
name: Tests focus on public contracts only
description: Tests must exercise public contracts (interfaces, public surface). Don't reach for InternalsVisibleTo, don't assert on implementation details, don't test internal helpers directly.
type: feedback
---

Tests verify that the public contract is satisfied — nothing more. Interfaces are the contract. The test cares that the contract holds; how the implementation achieves it is the implementation's concern.

**Why:** Tests that bind to internals couple the test suite to implementation choices. A refactor that preserves the public contract should not break tests; if it does, the tests were testing the wrong thing. `InternalsVisibleTo` is a smell that the test wanted to peek at something it shouldn't have. The contract being satisfied is what matters; the path the implementation took to satisfy it is not under test.

**How to apply:**
- Resolve concrete types under test through DI / interfaces, not by direct `new`-ing of internal classes.
- Don't add `InternalsVisibleTo` to make tests "easier." If a behavior is only visible through an internal hook, that behavior probably isn't part of the contract — drop the test or reformulate it against the public surface.
- Don't assert implementation-specific details (exact byte counts, exact algorithm choices, internal data-structure contents) unless the contract documents them.
- When time / clocks / random / external IO is involved, substitute via the standard BCL seams (`TimeProvider`, `HttpClient` factories, etc.) — that's dependency injection, not internals access.
- A behavior that is observable through the public contract (e.g. "tokens expire after their lifetime" → observable via `TryGet` returning false post-expiry) does not need an internal-helper test.
