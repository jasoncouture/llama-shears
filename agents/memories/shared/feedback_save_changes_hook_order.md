---
name: ISaveChangesHook order is undefined
description: Hooks run in undefined order; if two hooks need a defined sequence they belong in a single hook
type: feedback
---

`ISaveChangesHook` implementations in `LlamaShears.Data` are invoked by `SaveChangesHookInterceptor` in **undefined** order. The order is not specified, not guaranteed, and not guaranteed to be consistent across runs or versions. Any observed ordering (e.g. DI registration order today) is a side effect, not a contract.

If two pieces of work need a defined sequence, they must live inside a single hook. Two hooks that depend on each other are by definition mis-designed.

**Why:** Order-dependent hooks are a hidden coupling that turns a refactor (registration order, container, version bump) into a silent behavioral change. The user wants the rule to be enforced as a design constraint up front rather than discovered as a regression later. The fact that hooks happen to run in registration order today is an implementation detail.

**How to apply:**
- Each hook should operate on a disjoint slice of entity state. If you find yourself thinking "this hook depends on that hook having run," fold them together.
- Do not write or accept comments rationalising registration order ("X first so Y can see its result"). Such comments are a sign the design is wrong; merge the hooks.
- When reviewing a proposed hook, check whether the design assumes the entity has been mutated by a prior hook — that is the smell.
- Treat any future debugging that surfaces order-dependence as a bug in hook design, not in registration.
