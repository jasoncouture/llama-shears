using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Core;

public sealed record AgentHandle(SessionId Id, Task AgentTask, string ConfigHash, AsyncServiceScope Scope) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, AgentHandle> _children = new ConcurrentDictionary<Guid, AgentHandle>();
    public bool TryAddChild(AgentHandle child) => _children.TryAdd(child.Id.Id, child);
    public bool TryRemoveChild(Guid id, [NotNullWhen(true)] out AgentHandle? child) => _children.TryRemove(id, out child);
    public async ValueTask<bool> DestroyChild(Guid id)
    {
        if(id == Id.Id) throw new InvalidOperationException("Cannot destroy self, call dispose instead");
        if(TryRemoveChild(id, out var child))
        {
           await child.DisposeAsync();
           return true; 
        }

        foreach(var node in _children.Values)
        {
            if(await node.DestroyChild(id)) return true;
        }
        return false;
    }

    public async ValueTask DisposeAsync()
    {
        var keys = _children.Keys.ToArray();
        foreach (var child in keys)
        {
            if (!_children.TryRemove(child, out var handle)) continue;
            await handle.DisposeAsync();
        }
        await Scope.DisposeAsync();
        await AgentTask;
    }
}
