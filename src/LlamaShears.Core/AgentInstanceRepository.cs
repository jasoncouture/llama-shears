using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace LlamaShears.Core;

public sealed class AgentInstanceRepository : IAgentInstanceRepository, IDisposable
{
    private int _disposed;

    private readonly ConcurrentDictionary<Guid, AgentHandle> _agentHandles =
        new ConcurrentDictionary<Guid, AgentHandle>();

    private readonly ConcurrentDictionary<string, Guid> _agentDefaultSessions =
        new ConcurrentDictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<AgentHandle> GetAllAgents()
    {
        var roots = _agentHandles.Where(i => i.Value.SessionPath.IsRootSession)
            .Select(i => i.Value);
        foreach (var root in roots)
        {
            foreach (var child in DescendentsOf(root.SessionPath.Id))
            {
                yield return child;
            }

            yield return root;
        }
    }

    public bool TryGetDefaultSession(string agentId, out Guid id) => _agentDefaultSessions.TryGetValue(agentId, out id);

    public IEnumerable<Guid> GetAgentInstancesByName(string name) =>
        _agentHandles.Values.Where(i =>
                string.Equals(i.SessionPath.Current.Name, name, StringComparison.OrdinalIgnoreCase))
            .Select(i => i.SessionPath.Id);

    public IEnumerable<AgentHandle> DescendentsOf(Guid parentId)
    {
        if (!TryGetAgent(parentId, out var parentHandle)) yield break;
        var parentStack = new Stack<Guid>();
        var visited = new HashSet<Guid>();
        var outputStack = new Stack<AgentHandle>();
        visited.Add(parentId);
        parentStack.Push(parentId);
        var all = _agentHandles.Values.Where(i => i.SessionPath.RootId == parentHandle.SessionPath.RootId)
            .Where(i => !i.SessionPath.IsRootSession)
            .OrderBy(i => i.SessionPath.ParentId)
            .ToList();

        while (parentStack.Count > 0)
        {
            var next = parentStack.Pop();
            var startIndex = BinarySearch(all, next);
            if (startIndex < 0) continue;
            while (startIndex < all.Count && all[startIndex].SessionPath.ParentId == next)
            {
                var child = all[startIndex];
                startIndex++;

                if (!visited.Add(child.SessionPath.Id)) continue;
                parentStack.Push(child.SessionPath.Id);
                outputStack.Push(child);
            }
        }

        while (outputStack.Count > 0)
        {
            yield return outputStack.Pop();
        }
    }

    private int BinarySearch(List<AgentHandle> agents, Guid parentId)
    {
        int left = 0, right = agents.Count - 1, match = -1;
        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            int cmp = agents[mid].SessionPath.ParentId.CompareTo(parentId);

            if (cmp < 0) left = mid + 1;
            else if (cmp > 0) right = mid - 1;
            else
            {
                match = mid;
                right = mid - 1;
            }
        }

        return match != -1 ? match : ~left;
    }

    public void RemoveDescendents(Guid parentId, bool includeParent = true)
    {
        foreach (var descendent in DescendentsOf(parentId))
        {
            Remove(descendent.SessionPath.Id);
        }
    }

    public bool Remove(Guid id)
    {
        if (!_agentHandles.TryRemove(id, out var handle)) return false;
        if (handle.SessionPath.IsRootSession)
        {
            _agentDefaultSessions.TryRemove(handle.SessionPath.Current.AgentId, out _);
        }
        return true;
    }

    private void ThrowIfDisposed()
    {
        if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 0) return;
        throw new ObjectDisposedException(nameof(AgentInstanceRepository));
    }

    public bool TryGetAgent(Guid id, [NotNullWhen(true)] out AgentHandle? handle) =>
        _agentHandles.TryGetValue(id, out handle);

    public bool Remove(Guid id, [NotNullWhen(true)] out AgentHandle? handle)
    {
        if (_agentHandles.Values.Any(i => i.SessionPath.Id != id && i.SessionPath.ParentId == id))
            throw new InvalidOperationException(
                $"{id} cannot be removed because it has children, the children must be removed first");
        return _agentHandles.Remove(id, out handle);
    }

    public bool TryRemove(Guid id, [NotNullWhen(true)] out AgentHandle? handle)
    {
        handle = null;
        if (DescendentsOf(id).Any()) return false;
        return Remove(id, out handle);
    }

    public AgentHandle GetAgent(Guid id) => _agentHandles[id];

    public void AddAgent(AgentHandle handle)
    {
        ThrowIfDisposed();
        if (!handle.SessionPath.IsRootSession && !_agentHandles.ContainsKey(handle.SessionPath.ParentId))
            throw new InvalidOperationException(
                $"Agent session {handle.SessionPath.Current} could not be added, because it's parent is unknown");

        if (!handle.SessionPath.IsRootSession && !_agentHandles.ContainsKey(handle.SessionPath.RootId))
            throw new InvalidOperationException(
                $"Agent session {handle.SessionPath.Current} could not be added, because it's root is unknown");

        if (!_agentHandles.TryAdd(handle.SessionPath.Id, handle))
        {
            throw new InvalidOperationException($"Session {handle.SessionPath} already exists");
        }

        if (!handle.SessionPath.IsRootSession) return;
        _agentDefaultSessions[handle.SessionPath.Current.AgentId] = handle.SessionPath.Id;
    }

    private IEnumerable<Guid> BuildSessionPath(Guid id)
    {
        var visited = new HashSet<Guid>();
        var current = id;
        while (true)
        {
            if (visited.Count > 10)
            {
                throw new InvalidOperationException($"Session path depth was greater than 10 for session ID: {id}");
            }

            if (!visited.Add(current))
                throw new InvalidOperationException($"Circular reference detected for session ID {id}");
            yield return current;
            var handle = GetAgent(current);
            if (handle.SessionPath.IsRootSession) yield break;
            current = handle.SessionPath.ParentId;
        }
    }

    public AgentSessionPath GetAgentPath(Guid id) => new AgentSessionPath([..BuildSessionPath(id)]);

    public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
}
