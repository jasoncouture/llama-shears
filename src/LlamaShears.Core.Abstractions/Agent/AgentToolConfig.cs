using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Agent;

public sealed record AgentToolConfig(int TurnLimit = 8);
