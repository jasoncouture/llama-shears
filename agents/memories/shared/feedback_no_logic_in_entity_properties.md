---
name: No logic in entity property setters
description: Entity properties stay as plain auto-properties; validation and normalization belong in save-changes hooks
type: feedback
---

EF entity property setters in `LlamaShears.Data` must not contain logic — no validation, no normalization, no transformation. Properties are plain `{ get; init; }` or `{ get; set; }` auto-properties. Anything that needs to gate or rewrite a value at save time goes into an `ISaveChangesHook` implementation registered via `services.AddDatabaseHook<T>()`.

This applies even when "logic in the setter" is the textbook-correct way to enforce an invariant (e.g. "validate format and normalize to lowercase on assignment"). The user knows that pattern; they have rejected it for this codebase.

**Why:** The user dislikes the property-with-logic pattern strongly enough to say so explicitly. Beyond personal preference, putting all save-time invariants through the same hook mechanism keeps entities as inert data shapes, makes the enforcement points discoverable (search for `ISaveChangesHook` implementors), and avoids two competing places where a save-time rule might live.

**How to apply:**
- When asked for a validated/normalized property, write a plain auto-property on the entity and a per-entity `ISaveChangesHook` that does the validation/normalization, mutating via `entry.Property(name).CurrentValue = ...`.
- Do not write `{ get; init => field = Validate(value); }` patterns or backing-field setters with logic, even when the C# `field` keyword makes it look clean.
- `required` + `init` is still encouraged for set-once semantics — that is metadata, not logic.
- `IsRequired()` / `HasIndex().IsUnique()` and other fluent-API constraints stay in `ConfigureModel` as before; those are schema, not setter logic.
