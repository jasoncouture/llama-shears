using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Per-agent tool-loop guardrails.
/// </summary>
/// <param name="TurnLimit">Maximum number of consecutive tool-call turns the loop will take before forcing the model to produce a user-facing answer.</param>
public sealed record AgentToolConfig(int TurnLimit = 8);
