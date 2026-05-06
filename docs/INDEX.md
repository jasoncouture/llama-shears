# Project Documentation Index

This is the main entry point for all documentation topics. Each topic will have its own directory with an INDEX.md file.

## Topics

- [Design](design/INDEX.md): Heartbeat-based agentic host architecture and layering.
- [Data Layer](design/data.md): EF Core + SQLite, `IDataObject` / `IModifiableDataObject`, save-change interceptors, per-entity `IModelConfigurable<TSelf>`.
- [Tool Calling](design/tool-calling.md): tool catalog (local + MCP), polymorphic turn model, parallel execution, `ReportStatus`-as-terminator, per-agent iteration limits.
- [Heartbeat](design/heartbeat.md): per-agent autonomous wake-up — tick-checked timing, file-driven prompt, disabled at config (period ≤ 0) vs. silent at runtime (file empty/missing).
- [Architectural Decisions](adr/INDEX.md): ADRs covering analyzer policy, naming conventions, file layout, and other accepted constraints.
