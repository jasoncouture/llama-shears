using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Core;

/// <summary>
/// Owns the resources of one running agent instance: the DI scope, the captured
/// <see cref="System.Threading.ExecutionContext"/> the agent boots into, the underlying
/// <see cref="IAgent"/>'s background run-task, and any child <see cref="AgentHandle"/>s
/// spawned from it. Created cold by <see cref="IAgentFactory"/>; goes hot when <see cref="Start"/>
/// is invoked. Disposing tears down children first, then the scope, then awaits the run-task.
/// </summary>
/// <param name="SessionPath">Identity of this agent's session, including parent/root chain.</param>
/// <param name="ConfigHash">Hash of the <see cref="AgentConfig"/> the handle was built against.</param>
/// <param name="Scope">DI scope owned by the handle; disposed on teardown.</param>
/// <param name="ExecutionContext">Blank execution context the agent loop runs under.</param>
public sealed record AgentHandle(
    SessionPath SessionPath,
    string ConfigHash,
    AsyncServiceScope Scope,
    ExecutionContext ExecutionContext) : IAsyncDisposable
{
    /// <summary>Background task returned by <see cref="IAgent.RunAsync"/>; <see langword="null"/> until <see cref="Start"/> is called.</summary>
    public Task? AgentTask { get; private set; }

    private readonly ConcurrentDictionary<Guid, AgentHandle> _children = new ConcurrentDictionary<Guid, AgentHandle>();

    /// <summary>Attempts to attach <paramref name="child"/> under this handle; returns <see langword="false"/> if a child with the same id is already present.</summary>
    public bool TryAddChild(AgentHandle child) => _children.TryAdd(child.SessionPath.Id, child);

    /// <summary>Removes a direct child by id without disposing it.</summary>
    public bool TryRemoveChild(Guid id, [NotNullWhen(true)] out AgentHandle? child) =>
        _children.TryRemove(id, out child);

    /// <summary>
    /// Removes and disposes a descendant anywhere in this handle's subtree. Walks children
    /// recursively until the target is found.
    /// </summary>
    /// <returns><see langword="true"/> if the descendant was found and disposed.</returns>
    public async ValueTask<bool> DestroyChild(Guid id)
    {
        if (id == SessionPath.Id) throw new InvalidOperationException("Cannot destroy self, call dispose instead");
        if (TryRemoveChild(id, out var child))
        {
            await child.DisposeAsync();
            return true;
        }

        foreach (var node in _children.Values)
        {
            if (await node.DestroyChild(id)) return true;
        }

        return false;
    }

    /// <summary>
    /// Starts the agent loop on the captured execution context. Idempotent guard throws if called twice.
    /// </summary>
    public void Start()
    {
        if (Started) throw new InvalidOperationException("Agent has already been started");
        var currentExecutionContext = ExecutionContext.Capture();
        Debug.Assert(currentExecutionContext is not null);
        try
        {
            ExecutionContext.Restore(ExecutionContext);
            var agent = Scope.ServiceProvider.GetRequiredService<IAgent>();
            AgentTask = agent.RunAsync();
        }
        finally
        {
            ExecutionContext.Restore(currentExecutionContext);
        }
    }

    /// <summary><see langword="true"/> once <see cref="Start"/> has been called.</summary>
    public bool Started => AgentTask is not null;

    /// <summary><see langword="true"/> if started and the agent task has not yet completed.</summary>
    public bool Running => Started && !AgentTask!.IsCompleted;

    /// <summary>Disposes children first, then the scope, then awaits the run-task.</summary>
    public async ValueTask DisposeAsync()
    {
        var keys = _children.Keys.ToArray();
        foreach (var child in keys)
        {
            if (!_children.TryRemove(child, out var handle)) continue;
            await handle.DisposeAsync();
        }

        await Scope.DisposeAsync();
        if (AgentTask is not null) await AgentTask;
    }
}
