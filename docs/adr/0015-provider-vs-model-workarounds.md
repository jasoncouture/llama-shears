# ADR-0015: Provider workarounds are absorbed, model workarounds are not

Accepted 2026-04-28. Supersedes [ADR-0008](0008-no-per-model-workarounds.md).

## Context

ADR-0008 stated the framework would not absorb per-model workarounds, drawing the line between *idiosyncratic* and *structural* problems. That framing is directionally correct but leaves three things unstated, and one of those omissions is doing real work to obscure the actual reasoning:

1. **Provider quirks and model quirks are not the same thing**, and ADR-0008 conflates them. A provider exposes an API, a transport, a contract — those have shape, version skew, and edge cases that the framework genuinely has to handle to function (deserialization quirks, request/response shape drift, transport semantics). A model has *behaviour* — how it responds to prompts, how it formats tool calls, how it terminates, how it handles JSON. The first is plumbing the framework has to do anyway. The second is what ADR-0008 was actually trying to keep out, and the conflation invites future arguments about which side of the line a given fix is on.

2. **The reason for the line is volume control, not purism.** Any single model workaround is cheap. The user wants it, the fix is a few lines, the maintenance cost looks negligible. The cost is not in the individual; it is in the accumulation. Ten cheap workarounds are not ten units of cost — they are an ongoing tax on every framework change, every model rev, every reader trying to understand the call graph, and every test of "what does the framework actually do." A purist position ("never compromise") would be cleaner to enforce but worse for users; the boundary here is pragmatism applied to the *aggregate*, not the *individual* — which is exactly the form ADR-0009 takes when read in volume rather than per-decision.

3. **The escape hatch matters.** "Use a better model" is the user-facing answer when the framework declines a model workaround. That answer externalises cost onto the user, and it does so in tension with the README's "every provider first-class." It is still the right answer for the framework, because the framework cannot scale to chase every model. But the framework's longer-term answer is *plugins*: anyone will be able to replace, decorate, or otherwise tamper with most of the framework's machinery. If a user wants per-model surgery the framework declines to do inline, the plugin surface is where that surgery lives. The framework's job is to make sure the surface is sufficient.

ADR-0008 captures (1) implicitly and misses (2) and (3). That makes it read as the purist position the author does not actually hold. This ADR replaces it with the framing the author does hold.

## Decision

**Provider quirks are absorbed by the framework.** Differences between provider APIs, transports, contracts, deserialization shapes, error semantics, and version skew are framework concerns. Each provider implementation owns its own quirks; the abstraction is what shields callers from them. This is plumbing.

**Model quirks are not absorbed by the framework, with a single exception.** Behavioural quirks of a specific model — how it formats tool calls, how it handles JSON, how it interprets termination, what it does on retry — are the user's concern. The framework's response is "use a better model, configure your agent differently, or write a plugin." The exception is when a quirk is *very common across a large subset of models in active use*; in that case it is no longer a model quirk, it is a structural property of the language-model class, and the framework absorbs it the same way ADR-0008 did (iteration ceilings, structured-output primitives, required terminators, etc.).

**The "very common across a large subset" threshold is judgment-based and stays that way.** It is not defined as a percentage or a count. The author accepts that this means edge cases will be adjudicated by review rather than by rule, and that future-author is the one who has to hold the line. The alternative — pinning a number — would force the line to move every time the model landscape moves, and would produce more churn than it prevents.

**Plugins are the explicit escape hatch.** When a user has a model behaviour they want addressed and the framework declines to do it inline, the plugin surface is the supported answer. The framework's obligation in that direction is to keep the plugin surface broad enough that *most* such customisations are reachable from a plugin without modifying framework code. "Anyone who wants to do it can; the framework just won't do it inline."

Concrete shapes a plugin can take to address a model quirk:

- **Replacement provider.** Ship a provider that registers under the same plugin contract, presents the same surface to the agent, and internally does whatever model-specific surgery is needed. The framework sees a normal provider; the quirk is fully contained in the plugin.
- **Decoration.** Wrap an existing provider, mutating the input on the way in and/or the output on the way out. Useful when the underlying provider is otherwise correct and only one or two transformations need to happen at the edges (prompt rewriting, response sanitization, tool-call format normalization).
- **Other extension points.** Anywhere the framework exposes a seam — input/output channels, save-changes hooks, MCP tool handlers, agent configuration — a plugin can attach without touching framework code.

The contract from the framework's side: the seams are real, are documented, and are kept stable enough that plugins built against them do not have to chase framework refactors. The contract from the plugin's side: per-model fixes live there, not upstream.

**"Use a better model" is the immediate user-facing answer**, with the cost honestly named. Some users will be unable to switch (license, cost, hardware, local-only constraints). For those users, the answer is the plugin surface, not the inline fix. This is the deliberate trade.

## Consequences

- The framework's surface area is bounded by *aggregate* cost, not per-fix cost. Each individual workaround request is judged against the accumulation it implies, not against its own line count.
- Provider implementations are allowed to be messy where the provider is messy. That is the abstraction earning its keep.
- The framework's value proposition to users with a problematic model is "configuration, prompts, agent shape, model swap, plugin" — in roughly that order. "Inline framework fix" is not on the list.
- The plugin surface becomes load-bearing. Keeping it broad enough to address the cases the framework declines to address inline is now an explicit framework concern, not a "future nice-to-have."
- The README's "every provider first-class" framing remains accurate — providers are first-class. Models are not, by design.
- Adjudication of edge cases ("is this a structural problem or a model-specific one?") stays with the author and any future maintainers, by design. The cost of judgment-based lines is admitted up front.
- ADR-0008 is superseded. Its core direction (no per-model fixes inline) survives; the reasoning that was missing now lives here.

## Obligation distribution

The framework's obligation is to make external handling *possible* — to expose the seams, keep them stable, and document them well enough that a plugin author can address a model quirk without forking the codebase. The framework does **not** owe the fix itself. A user who runs into a model-specific problem and finds no built-in answer has not encountered a framework bug; they have encountered the boundary the framework declared on purpose.

The framework *may* choose to absorb a specific failure mode anyway, when the pattern is common enough that letting every user re-discover and re-solve it would be hostile. That choice is the framework spending its own surface-area budget; it is not a debt the framework was carrying.

A concrete example already planned: a per-agent turn limit and per-turn time limit on the agent loop, with the agent entering a cool-off until the next heartbeat (or, when heartbeat is disabled, until the next external interaction).

The reasoning chain that puts this fix inline is explicit: agents getting stuck in loops — tool-call cycles, repeated outputs, refusal to terminate — is a *nearly universal* failure mode. It is not a quirk of one model or one family; it is a property of the way autoregressive language models behave under tool-augmented prompting, and every framework that hosts such models will see it. That clears the "very common across a large subset of models in active use" threshold by a wide margin, which is what moves the fix from "user's problem, write a plugin" into "structural safeguard the framework owes its users." The same fix proposed as "Gemma loops on this prompt" would have been declined; proposed as "language models in general loop on tool-augmented prompts," it is in scope.

This is the test for any future inline absorption: not "is the fix small," not "would users like it," but "is the failure mode nearly universal across the models a user might plausibly run." If yes, the framework can choose to absorb it. If no, the seam exists for the user.

## Exceptions

Exceptions to the above will be made, and will probably be made more than once. The "very common across a large subset" rule is the *default* test for inline absorption; it is not the only path to it.

The rule's purpose is to prevent the slow accumulation of small per-model tweaks into a maintenance burden that dominates the framework over time. When a candidate fix clearly does not contribute to that pileup — because it is self-contained, has no per-model branching, does not invite a sibling fix for every other model, and does not have to be revisited every release — the pileup argument does not apply, and the rule should not be applied as if it did. In those cases the project falls back to its underlying goal: weigh the cost to the user against the cost to the project, choose the path that causes the least harm, and document the decision.

This is not a loophole. It is the recognition that the rule is a heuristic in service of an outcome, and when the rule and the outcome disagree, the outcome wins. ADR-0009 (pragmatism over technical purity) is the broader frame; this section is its application here. The bar for invoking an exception is not low — "it would be nice" does not clear it — but the bar exists.
