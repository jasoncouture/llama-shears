namespace LlamaShears.Data.Entities;

/// <summary>
/// Role of a participant in an LLM context exchange.
/// </summary>
public enum SessionMessageRole
{
    System,
    User,
    Assistant,
    Tool,
}
