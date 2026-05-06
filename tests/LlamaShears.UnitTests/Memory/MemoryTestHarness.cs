using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Memory;

internal sealed class MemoryTestHarness : IDisposable
{
    private MemoryTestHarness(
        string root,
        string agentId,
        SqliteMemoryService service,
        FakeTimeProvider time,
        VariableDimensionEmbeddingProviderFactory? variableDim)
    {
        Root = root;
        AgentId = agentId;
        Service = service;
        Time = time;
        VariableDim = variableDim;
    }

    public string Root { get; }

    public string AgentId { get; }

    public SqliteMemoryService Service { get; }

    public FakeTimeProvider Time { get; }

    public VariableDimensionEmbeddingProviderFactory? VariableDim { get; }

    public static MemoryTestHarness Create(string agentId = "test-agent")
        => CreateInternal(agentId, variableDim: null);

    public static MemoryTestHarness CreateWithVariableDimension(int initialDimensions, string agentId = "test-agent")
        => CreateInternal(agentId, new VariableDimensionEmbeddingProviderFactory(initialDimensions));

    private static MemoryTestHarness CreateInternal(string agentId, VariableDimensionEmbeddingProviderFactory? variableDim)
    {
        var root = Path.Combine(Path.GetTempPath(), "llamashears-memory-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var configs = Substitute.For<IAgentConfigProvider>();
        configs.GetConfigAsync(agentId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentConfig?>(new AgentConfig(
                Model: new AgentModelConfig(new ModelIdentity("STUB", "stub-chat")),
                Id: agentId,
                WorkspacePath: root,
                Embedding: new AgentEmbeddingConfig(new ModelIdentity("STUB", "stub-embed")))));

        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch.AddSeconds(1_700_000_000));
        var options = Options.Create(new MemoryServiceOptions());
        IEmbeddingProviderFactory[] factories = variableDim is null
            ? [new StubEmbeddingProviderFactory()]
            : [variableDim];
        var service = new SqliteMemoryService(
            configs,
            factories,
            time,
            options,
            NullLogger<SqliteMemoryService>.Instance);

        return new MemoryTestHarness(root, agentId, service, time, variableDim);
    }

    public string PathOf(params string[] parts) => Path.Combine([Root, .. parts]);

    public void Dispose()
    {
        try
        {
            // Microsoft.Data.Sqlite holds the file open via its connection
            // pool; clear it before deleting the workspace, otherwise
            // Linux is happy and Windows isn't.
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }
}
