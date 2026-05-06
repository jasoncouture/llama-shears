# Project Documentation Index

This is the main entry point for all documentation topics. Each topic will have its own directory with an INDEX.md file.

## Topics

- [Design](design/INDEX.md): Heartbeat-based agentic host architecture and layering.
- [Tool Calling](design/tool-calling.md): tool catalog (local + MCP), polymorphic turn model, parallel execution, `ReportStatus`-as-terminator, per-agent iteration limits.
- [Heartbeat](design/heartbeat.md): per-agent autonomous wake-up — tick-checked timing, file-driven prompt, disabled at config (period ≤ 0) vs. silent at runtime (file empty/missing).
- [Agent Workspace](design/agent-workspace.md): per-agent home directory; conventional files (`BOOTSTRAP.md`, `IDENTITY.md`, `SOUL.md`, `USER.md`, `HEARTBEAT.md`, `TOOLS.md`, `MEMORY.md`, `memories/`) and what the framework does with them.
- [Architectural Decisions](adr/INDEX.md): ADRs covering analyzer policy, naming conventions, file layout, and other accepted constraints.
