# Principles

This document captures the two principles that generate most of the project's concrete rules. The ADRs in [docs/adr/](docs/adr/) are individual, falsifiable decisions; this document is the layer above them — the operating philosophy from which those decisions follow.

It is intentional that this layer is stated explicitly rather than left implicit in the ADR sequence. Implicit principles get rediscovered, re-argued, and gradually drift; explicit ones can be cited, challenged, and revised on purpose. The redundancy between this document and ADR-0007 / ADR-0009 is deliberate — the ADRs articulate these ideas at the point they were first accepted as decisions, and this document distills them as the generative layer. Both are load-bearing.

## 1. Mechanism over memory

Policy that depends on someone remembering it is policy that decays. People get tired, processes drift, code reviews miss things, and the tenth person reading the codebase does not have the first person's context. The project's job is to convert intent into *mechanism* — something the compiler, the analyzer, the type system, the API shape, the build, or the default value enforces automatically.

The order of preference, strongest to weakest:

1. **Make the wrong thing impossible.** Type signatures, sealed APIs, structural constraints, build errors.
2. **Make the wrong thing fail loudly at the boundary it crosses.** Analyzer errors, runtime exceptions on misuse, fail-fast configuration. Distinguish "didn't try" from "tried and failed."
3. **Make the wrong thing inconvenient.** Verbose-but-correct over terse-but-trap-laden; defaults biased toward the right answer; opinionated APIs that route the easy path through the correct one.
4. **Document the convention.** The fallback when none of the above work, and a maintenance liability — not a tool.

Reaching for level 4 is admitting that no mechanism captured the intent. That admission should be conscious, not casual. Anything sitting in level 4 should migrate up the stack the moment a mechanism becomes available.

The corollary: a rule the project enforces *only* by review or by documentation is a rule that will eventually be broken. Either accept that and lower its priority, or move it into mechanism.

## 2. Bound aggregate cost, not per-decision cost

Every individual yes looks cheap. The cost is in the trajectory, not the line count. The question on any addition is not "what does this one cost" but "if I say yes to this, what does the aggregate look like in two years, after the next twelve cases just like it?"

A cheap individual fix that invites three more cheap individual fixes per release is not cheap. A convenience method that becomes the seventh way to do the same thing is not a convenience. A small exception to a rule is not small if it sets the precedent for fifty more. Per-decision economics lie about this; aggregate economics do not.

The escape valve is not "say no to everything." It is:

- Refuse the inline addition when it grows the core's permanent surface for a case-specific reason.
- Provide a seam — a plugin, a decorator, an extension point — through which the case can be addressed externally without expanding the core.
- Externalize cost when externalizing it is honest about who bears it. ("Use a better model" is honest; pretending no one pays is not.)

The framework's surface area stays bounded by what the framework structurally owes. The seams take the rest.

## The tension: never form over function

Mechanism over memory leans pure. Bound aggregate cost leans practical. The two are not always in agreement.

The point is not that purity is bad and practicality wins, nor the reverse. The point is that purity divorced from practicality stops being purity in any useful sense and becomes aesthetic preference defended with a principle, and practicality divorced from purity stops being practicality and becomes ergonomic erosion of the long-term shape. The mandates that earn their place are the ones that are *both* pure *and* practical.

**Form has to do work.** Form that doesn't do work is decoration. Function that ignores form is debt waiting to be named. A rule that exists for its own elegance, or because the author likes how it looks, or because some other codebase does it that way, has not earned its place yet. A shortcut that papers over a structural problem has not paid its bill yet. Both pay later, and both pay more than they would have paid up front.

When a decision is hard, the question is not "which principle applies here" but "what does this cost the project's long-term shape, and what does it cost the user today?" Both costs are real. Neither one wins by default. The decision goes in an ADR, with the trade named.

## How this shows up in the project

The ADRs are concrete instances of these principles applied to specific decisions. The mapping is not exclusive — most ADRs touch both — but the dominant generator is usually clear.

- **Mostly mechanism over memory:** ADR-0001 through ADR-0006 (analyzer-enforced naming, layout, and disambiguation rules), ADR-0011 (config root location), ADR-0013 (`DateTime` requires justification), ADR-0014 (source-generated logging is the default).
- **Mostly bound aggregate cost:** ADR-0010 (exception handling requires justification), ADR-0012 (XML doc comments default absent — every comment is a maintenance commitment), ADR-0015 (provider vs. model workarounds).
- **Both at once, with the tension named:** ADR-0007 (pit of success — what mechanism over memory looks like when applied broadly), ADR-0009 (pragmatism over technical purity — what bounding aggregate cost looks like when held against purist instinct).

The save-changes hook pattern, the no-navigation-properties EF rule, the no-logic-in-property-setters rule, the source-generated logging requirement, the loud-failures-over-silent-success memory, and the plugin-as-escape-hatch posture in the agent host are all instances of the same two principles, applied at different layers.

## What this document is not

- It is not a rule. The rules are in the ADRs and the analyzers. This document explains *why* those exist, not *what* they say.
- It is not a style guide. Style is in [CONTRIBUTING.md](CONTRIBUTING.md).
- It is not aspirational. Behavior in the codebase that contradicts these principles is either a bug, an explicit deviation that has earned its place via ADR, or a signal that the principles themselves need revisiting — in that order of likelihood.
- It is not immutable. Unlike ADRs, this document is revised in place when the underlying philosophy changes. ADRs are a record of decisions at a point in time; this is a description of the operating philosophy now. If the philosophy shifts, the document shifts with it, and the diff is the record.
