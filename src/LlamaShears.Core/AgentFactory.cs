using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Core;

internal sealed class AgentFactory : IAgentFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDataContextFactory _dataContextFactory;

    public AgentFactory(IServiceScopeFactory scopeFactory, IDataContextFactory dataContextFactory)
    {
        _scopeFactory = scopeFactory;
        _dataContextFactory = dataContextFactory;
    }

    public async ValueTask<AgentHandle> CreateAgentAsync(AgentConfig config, SessionId session,
        IEnumerable<KeyValuePair<string, object?>> data, CancellationToken cancellationToken)
    {
        var sessionPath = _dataContextFactory.Current.GetSessionPath().CreateChildSession(session);
        return await CreateAgentAsync(config, sessionPath, data, cancellationToken);
    }

    public async ValueTask<AgentHandle> CreateAgentAsync(AgentConfig config, SessionPath sessionPath,
        IEnumerable<KeyValuePair<string, object?>> data, CancellationToken cancellationToken)
    {
        var previousContext = ExecutionContext.Capture();
        var agentExecutionContext = await CreateAgentExecutionContext();
        ExecutionContext.Restore(agentExecutionContext);
        try
        {
            var globals = CreateAgentDataContextGlobals(config, sessionPath, data);
            var (scope, agentContext) = await CreateAgentServiceScope(sessionPath, globals, cancellationToken);
            // Must re-capture the execution context
            return new AgentHandle(sessionPath, config.Hash, scope, agentContext);
        }
        finally
        {
            ExecutionContext.Restore(previousContext!);
        }
    }

    private async ValueTask<(AsyncServiceScope, ExecutionContext)> CreateAgentServiceScope(
        SessionPath sessionPath,        
        Dictionary<string, object?> globals,
        CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        try
        {
            var dataContextFactory = scope.ServiceProvider.GetRequiredService<IDataContextFactory>();
            dataContextFactory.CreateContext(sessionPath.Current);
            var dataProviders = scope.ServiceProvider.GetScopedDataProviders();
            await dataContextFactory.InitializeAsync(sessionPath.Current, dataProviders, globals, cancellationToken);
            // Resolve critical services now, so we fail fast.
            _ = scope.ServiceProvider.GetRequiredService<ILanguageModel>();
            _ = scope.ServiceProvider.GetRequiredService<IAgent>();
            // Because we transitioned to async, the parent scope may not pick up what we've just done.
            return (scope, ExecutionContext.Capture()!);
        }
        catch
        {
            await scope.DisposeAsync();
            throw;
        }
    }

    private static Dictionary<string, object?> CreateAgentDataContextGlobals(AgentConfig config,
        SessionPath sessionPath, IEnumerable<KeyValuePair<string, object?>> data)
    {
        var globals = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in data)
        {
            globals[pair.Key] = pair.Value;
        }

        // These are authoratative, not to be overwriten by the caller.
        config.ApplyTo(globals);
        config.Model.ApplyTo(globals);
        sessionPath.ApplyTo(globals);
        return globals;
    }

    private async ValueTask<ExecutionContext> CreateAgentExecutionContext()
    {
        var callerContext = ExecutionContext.Capture()!;
        var uiCulture = Thread.CurrentThread.CurrentUICulture;
        var culture = Thread.CurrentThread.CurrentCulture;
        var agentExecutionContext = await ExecutionState.CreateBlankContextAsync();
        // Switch to the fresh execution context, and copy the culture info from the parent
        ExecutionContext.Restore(agentExecutionContext);
        try
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = uiCulture;
            var capturedContext = ExecutionContext.Capture()!;
            return capturedContext;
        }
        finally
        {
            ExecutionContext.Restore(callerContext);
        }
    }
}
