using LlamaShears.Api.Tools.ModelContextProtocol.Subagent;
using LlamaShears.Core.Abstractions.Agent;
using NSubstitute;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Subagent;

public sealed class SubagentToolsTests
{
    [Test]
    public async Task ForwardsArgumentsToTheRunner()
    {
        var runner = Substitute.For<ISubagentRunner>();
        SubagentRunRequest? captured = null;
        runner
            .RunAsync(Arg.Any<SubagentRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.Arg<SubagentRunRequest>();
                return new ValueTask<SubagentRunResult>(
                    new SubagentRunResult(
                        Ok: true,
                        SessionId: "alice:00000000-0000-0000-0000-000000000001:subagent-x",
                        Awaited: true,
                        Output: "done",
                        TimedOut: false,
                        Error: null,
                        Started: true));
            });
        var tool = new SubagentTools(runner);
        using var cts = new CancellationTokenSource();

        var result = await tool.RunSubagent(
            prompt: "summarize the notes",
            model: "ollama/llama3",
            context: "keep it short",
            maxTurns: 4,
            awaitResult: true,
            timeoutSeconds: 30,
            cancellationToken: cts.Token);

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Prompt).IsEqualTo("summarize the notes");
        await Assert.That(captured.Model).IsEqualTo("ollama/llama3");
        await Assert.That(captured.Context).IsEqualTo("keep it short");
        await Assert.That(captured.MaxTurns).IsEqualTo(4);
        await Assert.That(captured.AwaitResult).IsTrue();
        await Assert.That(captured.TimeoutSeconds).IsEqualTo(30);
        await Assert.That(result.Ok).IsTrue();
        await Assert.That(result.Output).IsEqualTo("done");
        await runner.Received(1).RunAsync(Arg.Any<SubagentRunRequest>(), cts.Token);
    }

    [Test]
    public async Task DefaultsAwaitAndTimeout()
    {
        var runner = Substitute.For<ISubagentRunner>();
        SubagentRunRequest? captured = null;
        runner
            .RunAsync(Arg.Any<SubagentRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.Arg<SubagentRunRequest>();
                return new ValueTask<SubagentRunResult>(
                    new SubagentRunResult(false, null, false, null, false, null));
            });
        var tool = new SubagentTools(runner);

        await tool.RunSubagent("go");

        await Assert.That(captured!.AwaitResult).IsTrue();
        await Assert.That(captured.TimeoutSeconds).IsEqualTo(SubagentRunRequest.DefaultTimeoutSeconds);
        await Assert.That(captured.MaxTurns).IsNull();
        await Assert.That(captured.Model).IsNull();
        await Assert.That(captured.Context).IsNull();
    }

    [Test]
    public async Task FireAndForgetMapsAwaitFalse()
    {
        var runner = Substitute.For<ISubagentRunner>();
        SubagentRunRequest? captured = null;
        runner
            .RunAsync(Arg.Any<SubagentRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.Arg<SubagentRunRequest>();
                return new ValueTask<SubagentRunResult>(
                    new SubagentRunResult(
                        Ok: false,
                        SessionId: "alice:00000000-0000-0000-0000-000000000001:subagent-x",
                        Awaited: false,
                        Output: null,
                        TimedOut: false,
                        Error: null,
                        Started: true));
            });
        var tool = new SubagentTools(runner);

        var result = await tool.RunSubagent("go", awaitResult: false);

        await Assert.That(captured!.AwaitResult).IsFalse();
        await Assert.That(result.Started).IsTrue();
        await Assert.That(result.Awaited).IsFalse();
    }
}
