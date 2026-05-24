namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Per-agent security/behavior gate applied during skill and MCP tool discovery.
/// Each <see cref="FilterOptions"/> sub-block independently gates a subsystem
/// (skills, tool invocation, tool sources) via a policy + name list.
/// </summary>
/// <param name="Skills">Filter applied to skill names returned by the skill repository.</param>
/// <param name="Tools">Filter applied to MCP tool names returned by tool discovery.</param>
/// <param name="Sources">Filter applied to MCP source / server names before any of their tools are discovered.</param>
public sealed record AgentBehaviorOptions(FilterOptions Skills, FilterOptions Tools, FilterOptions Sources);
