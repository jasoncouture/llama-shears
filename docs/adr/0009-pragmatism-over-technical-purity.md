# ADR-0009: Pragmatism over technical purity

Accepted 2026-04-28.

## Context

There is a recurring pattern in software design where a contributor identifies a "technically correct" position and then encodes it as a hard requirement. The position is usually defensible in isolation — there are real reasons someone arrived at it. But a position that is right for the contributor's environment, taste, and assumptions is not automatically right for every user the project will ever have.

The canonical example is the Go ecosystem's frequent stance that "all configuration must come from environment variables." It is technically defensible: env vars are widely supported, easy to inject in containers, naturally namespaced by process, etc. It is also operationally hostile to a real subset of users — for instance, macOS users running GUI-launched applications, where setting durable, application-visible environment variables is awkward, fragile, and full of surprises (`launchctl setenv` is per-session, shell rc files don't apply to GUI launches, and so on). The "right" answer (env vars only) produces a worse outcome for those users than a slightly less-pure answer (env vars *or* a config file) that accommodates them.

The error here is treating *technical correctness in the abstract* as terminating the design conversation. It does not. The conversation also has to weigh:

- The friction the mandate imposes on real users in real environments.
- The value the mandate actually delivers, beyond aesthetic consistency.
- Whether the alternative the mandate forbids has its own legitimate reasons to exist.
- Whether the project has the standing to refuse to support the alternative — usually it does not.

When users push back on a mandate and the response is "you're holding it wrong," the project is no longer being designed for users. It is being designed for the contributor's sense of correctness, at the user's expense.

## Decision

Mandates are avoided. When a "you must do it this way" position is proposed, it must justify itself on **user- or developer-facing value**, not on technical aesthetics or consistency.

Concretely:

- **Defaults can be opinionated.** The project may strongly prefer one approach, document it as the recommended path, ship the easy-path tooling for it, and surface it first in documentation. Opinionated defaults are part of pit-of-success ([ADR-0007](0007-pit-of-success.md)).
- **Mandates require a reason.** A constraint that forbids an alternative needs a real justification beyond "the preferred way is technically nicer." Acceptable justifications include security, correctness-under-concurrency, legal/compliance constraints, ABI stability ([ADR-0002](0002-no-public-fields.md)), eliminating ambiguity ([ADR-0001](0001-no-this-qualifier.md), [ADR-0003](0003-underscore-prefix-for-fields.md)), and similar load-bearing reasons.
- **Aesthetic preferences are not justifications.** "It's cleaner this way" or "it's how everyone else does it" or "the alternative offends my sensibilities" do not by themselves justify a mandate. They justify a recommendation.
- **The user's environment counts.** If the technically-correct path imposes meaningful friction on a real subset of users — macOS users for env-var-only config, Windows users for POSIX-only path conventions, offline users for cloud-only auth flows — the design has to weigh that friction explicitly. "Their platform is the problem" is not a serious answer.
- **When in doubt, leave the door open.** Provide the recommended path *and* a fallback. The fallback can be louder, less polished, or explicitly marked as the secondary route. Its existence is what matters.

The bar for adding a mandate is "would a reasonable user, in a reasonable environment, with a reasonable use case, be unable to use this project because of the constraint?" If yes — and the constraint is not load-bearing for one of the categories above — the constraint is wrong.

## Consequences

- Some surfaces will have multiple paths to the same outcome (config from env vars *and* a file; CLI args *and* a config; an opinionated API *and* an escape hatch). That is a feature, not duplication to be cleaned up.
- The project will not be maximally consistent. It will be maximally usable. The trade is intentional.
- New constraints get harder to add. A reviewer is entitled to ask "what user- or developer-facing value does mandating this provide, and what's the cost to users for whom the mandate is friction?" "It's the right way" is not a complete answer.
- This ADR does not invalidate the project's existing mandates. The accepted hard rules ([ADR-0001](0001-no-this-qualifier.md) through [ADR-0005](0005-one-type-per-file.md)) are mandates with concrete user- and developer-facing justifications: ABI stability, ambiguity elimination, navigability. They pass the bar. New mandates have to clear the same bar.
- The framework's design conversations explicitly weigh the friction a constraint imposes on users in environments the contributor may not personally use.

This ADR pairs with [ADR-0007 (Pit of success)](0007-pit-of-success.md) and [ADR-0008 (No per-model workarounds)](0008-no-per-model-workarounds.md): pit-of-success says the easy path should be the right path; ADR-0008 scopes which class of problems shape that path; this ADR says the path's *correctness* is judged by the user's outcome, not by the contributor's sense of taste.
