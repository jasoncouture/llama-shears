using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Api.Web.Services;

public sealed class DataContextInspector : IDataContextInspector
{
    private readonly IDataContextFactory _factory;

    public DataContextInspector(IDataContextFactory factory)
    {
        _factory = factory;
    }

    public DataContextInspection Inspect(SessionId sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        var alreadyCurrent = _factory.Current is { } current && Equals(current.Key, sessionId);
        var joined = false;
        try
        {
            if (!_factory.TryJoinContextScope(sessionId, out var scope))
            {
                return new DataContextInspection(
                    sessionId.ToString(),
                    [],
                    "No live data context for this session.");
            }

            joined = !alreadyCurrent;
            var snapshot = scope.Snapshot();
            var entries = snapshot
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => new DataContextEntry(
                    pair.Key,
                    pair.Value?.GetType().Name ?? "null",
                    DataContextValueFormatter.Format(pair.Value)));
            return new DataContextInspection(sessionId.ToString(), [.. entries], null);
        }
        catch (Exception ex)
        {
            return new DataContextInspection(sessionId.ToString(), [], ex.Message);
        }
        finally
        {
            if (joined)
            {
                _factory.ClearCurrent(false);
            }
        }
    }
}
