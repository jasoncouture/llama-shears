using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent.Todo;
using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Todo;

public sealed record TodoListResult(
    TodoResultState State,
    int ItemCount,
    ImmutableArray<TodoItem> Items,
    string? Error = null) : IToolResponse;
