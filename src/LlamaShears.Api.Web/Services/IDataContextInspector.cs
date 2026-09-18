using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Api.Web.Services;

/// <summary>
/// Snapshots the data-context bag for a chat session without leaving
/// that scope as <see cref="IDataContextFactory.Current"/> unless it
/// already was.
/// </summary>
public interface IDataContextInspector
{
    /// <summary>
    /// Returns a pretty-printed snapshot of <paramref name="sessionId"/>'s
    /// live scope. Missing scopes and join conflicts are reported on
    /// <see cref="DataContextInspection.Error"/> rather than thrown.
    /// </summary>
    DataContextInspection Inspect(SessionId sessionId);
}
