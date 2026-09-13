---
name: System prompts do not catalog tools
description: DEFAULT.md and other system/ephemeral templates must not outline individual tools; the model already receives the tool list
type: feedback
---

Do not add "when X, call `llamashears__foo`" (or any named-tool advertisement) to `DEFAULT.md` or other system / ephemeral templates. The harness already sends the tool catalog on the request; repeating it burns tokens and goes stale the next time a tool is added or renamed.

Tool *protocol* can stay: naming (`server__tool`), parallel calls, prefer a first-class tool over asking the user to run CLI, do not fabricate tool names. Skill catalog injection is not a tool outline — skills are not in the MCP list. Do not advertise individual MCP tools or their when-to-use rules in the system prompt.
