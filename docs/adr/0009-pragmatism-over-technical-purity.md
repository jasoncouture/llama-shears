# ADR-0009: Strive for technical purity, where practical

Accepted 2026-04-28.

## Context

Technical purity — consistency, predictability, principled mandates that eliminate whole classes of error — is a real virtue. The project's accepted mandate ADRs are all purity decisions, and each one earns its place by delivering a concrete improvement:

- [ADR-0001 (No `this.` qualifier)](0001-no-this-qualifier.md) and [ADR-0003 (underscore-prefix on fields)](0003-underscore-prefix-for-fields.md) eliminate name-resolution ambiguity at the source.
- [ADR-0002 (No public or internal fields)](0002-no-public-fields.md) eliminates ABI fragility.
- [ADR-0004 (No primary constructors on non-record types)](0004-no-primary-constructors-on-non-record-types.md) eliminates a silent-mutability hazard.
- [ADR-0005 (One top-level type per file)](0005-one-type-per-file.md) eliminates navigation ambiguity.

Each is a "you must do it this way" position the project takes deliberately, because the alternative is measurably worse for the people writing or reading the code. Purity here is not aesthetic; it is load-bearing.

Purity has a failure mode, though: it can be pursued past the point where it serves users. A canonical case is the stance — disproportionately concentrated in Go-written daemons and CLI tools, though not exclusive to that ecosystem — that "all configuration must come from environment variables." It is defensible in the abstract: env vars are widely supported, cleanly namespaced, easy to inject in containers, and they fit the 12-factor app rhetoric the cloud-native era has internalised. It is also operationally hostile to a real subset of users. macOS GUI-launched applications cannot easily see environment variables set in shell rc files; `launchctl setenv` is per-session and fragile; persistent values require launchd plists that the project's setup documentation typically does not mention. A user on macOS configuring such a tool discovers that the "right way" does not work, and the project's response — when it has one — is to tell the user to fix their environment, not to accept a config file. The technically-pure mandate has crossed into impracticality, and the project has chosen aesthetic consistency over the user's outcome.

Contrast the .NET ecosystem's idiomatic config pattern, which is layered: `appsettings.json` first, then `appsettings.<Environment>.json` overrides, then environment variables, then command-line arguments, with each layer overriding the previous. The user can configure by file, by environment, by arguments, or any combination — each form is a first-class citizen. The accommodation cost is low — `Microsoft.Extensions.Configuration` ships in the box — and the cultural expectation is accommodation rather than mandate. Node and Python tend in the same direction, treating config files and the environment as parallel inputs rather than competing ideologies. Purity-over-practicality projects exist in every ecosystem; the cultural concentration of "env vars only, your platform is the problem" is most visible in Go.

The point is not that purity is bad. The point is that purity divorced from practicality stops being purity in any useful sense and becomes aesthetic preference defended with a principle. The mandates that earn their place over time are the ones that are *both* pure *and* practical.

## Decision

The project strives for technical purity. The default design instinct is the principled, consistent, technically-correct approach. Mandates that eliminate whole classes of error or fragility are a feature, not a problem.

**Mandates can be proposed and accepted on technical-purity grounds alone.** A new mandate does not have to prove its practicality in advance. Practicality is a *challenge mechanism*, not a precondition for adoption. Mandates earn their staying power through use.

**Practicality challenges are legitimate and the project takes them seriously.** When a real user, in a real environment, with a real use case in scope for the project, hits friction caused by a mandate, the question becomes:

> Does the friction this mandate imposes outweigh the cost of accommodating the use case?

Several factors weigh into that decision:

- **How real the friction is.** "A hypothetical user might..." does not count. "Here is a class of users who can't get past it" does.
- **How load-bearing the mandate is.** Mandates that exist for security, correctness, ABI stability, ambiguity elimination, or similar concrete reasons carry a higher accommodation bar than mandates whose only justification is aesthetic consistency.
- **The cost of accommodating.** Some accommodations are cheap — accept a config file alongside env vars; provide an escape hatch alongside the opinionated API. Some are prohibitive: running the project on an ESP32 would require fundamentally different memory, threading, and dependency assumptions; the cost is not "a few extra lines" but a separate project.
- **Whether the use case is in scope at all.** "I just want to chat with an AI" against a project that exists to host autonomous agents is asking the project to be something it isn't. There are good chat tools — use them. That is not friction the project caused, and the project does not have to absorb it.

**When the project chooses not to accommodate, the rationale must be explicit and objective.** Acceptable reasons include:

- The technical cost of accommodation is disproportionate to the friction relieved.
- The use case is outside the project's stated scope.
- The accommodation would compromise a load-bearing mandate (security, correctness, ABI stability, etc.).
- The platform or environment is not in the project's target set, and adapting would require fundamentally different architecture rather than a configuration option.

"That's not how we do it" is not a reason. Neither is silent refusal. When the project declines an accommodation, the project says so, with the reason, in writing — so the user can understand the boundary and route around it.

**When a previously-accepted mandate becomes impractical** — because the world changes, a new platform emerges, or a use case within scope but unanticipated arrives — the ADR is revised. Revisiting an accepted ADR is expected behaviour, not a failure of resolve.

**Aesthetic consistency without a load-bearing reason is not technical purity.** "It's cleaner this way" or "it's the convention" do not by themselves justify a mandate that survives a serious practicality challenge. The accepted mandate ADRs each carry concrete justification; new mandates have to do the same once the challenges arrive.

## Consequences

- The project's defaults are opinionated and pure where pure pays.
- Mandates can be made up front; they have to survive practicality challenges to stay.
- The bar for *keeping* a mandate is "is this technically pure *and* does the friction it imposes not outweigh the cost of accommodating?"
- The project's response to a friction report is "weigh, decide, explain" — not "tough" and not "we'll do whatever you want."
- Some surfaces will have multiple paths to the same outcome because pure-only would create friction worth more than the accommodation cost.
- A mandate that was right yesterday but isn't right today gets revised. The existing accepted mandates ([ADR-0001](0001-no-this-qualifier.md) through [ADR-0005](0005-one-type-per-file.md)) all currently clear the bar; if any one stops, the right response is to revise it, not to defend it.
- Some asks will be declined — because the accommodation cost is too high, the use case is out of scope, or the platform is not in the project's target set. Those declinings come with explicit rationale, not dismissal.

This ADR pairs with [ADR-0007 (Pit of success)](0007-pit-of-success.md) and [ADR-0008 (No per-model workarounds)](0008-no-per-model-workarounds.md): pit-of-success says the easy path should be the right path; ADR-0008 scopes which class of problems shape that path; this ADR scopes the *kind* of "right" that's worth pursuing — the kind that's both technically defensible and weighed against legitimate user friction with explicit, objective reasoning.
