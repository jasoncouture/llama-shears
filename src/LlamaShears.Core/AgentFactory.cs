using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Core;

internal sealed class AgentFactory : IAgentFactory
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AgentFactory(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<AgentHandle> StartAgentAsync(
        AgentConfig config,
        SessionId session,
        IEnumerable<KeyValuePair<string, object?>> data,
        CancellationToken cancellationToken)
    {
        var previousContext = ExecutionContext.Capture();
        try
        {
            var uiCulture = Thread.CurrentThread.CurrentUICulture;
            var culture = Thread.CurrentThread.CurrentCulture;
            var blankExecutionContext = await ExecutionState.CreateBlankContextAsync();

            ExecutionContext.Restore(blankExecutionContext);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = uiCulture;

            var globals = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in data)
            {
                globals[pair.Key] = pair.Value;
            }
            
            // These are authoratative, not to be overwriten by the caller.
            globals[AgentConfig.DataKey] = config;
            globals[ModelConfiguration.DataKey] = config.Model;
            globals[SessionId.DataKey] = session;

            var scope = _scopeFactory.CreateAsyncScope();
            try
            {
                var dataContextFactory = scope.ServiceProvider.GetRequiredService<IDataContextFactory>();
                dataContextFactory.CreateContext(config.Id);
                var dataProviders = scope.ServiceProvider.GetScopedDataProviders();
                await dataContextFactory.InitializeAsync(config.Id, dataProviders, globals, cancellationToken);
                _ = scope.ServiceProvider.GetRequiredService<ILanguageModel>();
                var agent = scope.ServiceProvider.GetRequiredService<IAgent>();
                var runTask = agent.RunAsync();

                return new AgentHandle(session, runTask, config.Hash, scope);
            }
            catch
            {
                await scope.DisposeAsync();
                throw;
            }
        }
        finally
        {
            ExecutionContext.Restore(previousContext!);
        }
    }
}
