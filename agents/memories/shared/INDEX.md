# Shared memory index

- [Atomic commits and full-solution build/test](feedback_build_and_commit.md) — commit each logical change; always run `dotnet build` and `dotnet test` with no params
- [Never accept IConfiguration as a parameter](feedback_iconfiguration_parameter.md) — use `AddOptions<T>().BindConfiguration("Section")`; expose only the section name (defaulted, last)
- ["SDK reference" means FrameworkReference, not the Sdk attribute](feedback_sdk_reference.md) — add `<FrameworkReference Include="..." />`, never swap the project's `Sdk=`
- [No EF navigation properties; relationships on principal side](feedback_no_ef_navigation_properties.md) — entities expose scalars + FK columns only; relationship config lives on the referenced entity, giving a "who points at me" view per principal
- [ISaveChangesHook order is undefined](feedback_save_changes_hook_order.md) — hooks run in undefined order; order-dependent hooks belong combined into one hook
- [No logic in entity property setters](feedback_no_logic_in_entity_properties.md) — entity properties stay plain; validation and normalization live in `ISaveChangesHook` implementations
- [Wait for approval after opinion questions](feedback_wait_on_opinion_questions.md) — "what do you think?" / "perhaps we should…" → give opinion and stop, even in auto mode
- [Prefer collection expressions](feedback_collection_expressions.md) — use `[a, b]` over `ImmutableArray.Create`, `new List<>()`, `new[] { ... }`, etc.
- [Always use DateTimeOffset](feedback_always_datetimeoffset.md) — every timestamp is `DateTimeOffset`; convert at the boundary when an API returns `DateTime`
- [Source-generated logging is the default](feedback_source_generated_logging.md) — use `[LoggerMessage]` partial methods; direct `ILogger.LogX` is a code smell (CA1873)
- [Vector store choice — Microsoft.Extensions.VectorData](project_vector_store.md) — when vector storage is added, use `Microsoft.Extensions.VectorData` with the SQLite (sqlite-vec) connector; not yet implemented
- [Self-registering DI helpers](feedback_self_registering_di_helpers.md) — per-item registrations call their companion infrastructure registration so callers can't forget to wire the consumer
- [Provider.Abstractions is the core](project_provider_abstractions_is_core.md) — `Provider.Abstractions` is the de-facto plugin contract; will become NuGet-shipped (AssemblyLoadContext); don't rename or split preemptively
- [Agent config lifecycle](project_agent_config_lifecycle.md) — disk → `AgentManager` (lifecycle, scan top-level `agents/*.json` on tick) → agent provider (read API); configs are immutable snapshots; in-flight interactions see one snapshot end-to-end
